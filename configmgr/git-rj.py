#!/usr/bin/env python3

import argparse
import concurrent.futures
import json
import os
import platform
import re
import shutil
import subprocess  # Python 3.7 or later
import sys
import threading
import time

from pathlib import Path
import xml.etree.ElementTree as ET

# Global Configuration
VERSION = "1.0-alpha.20211020"
GITDEBUGLEVEL = 0
MAX_WORKERS = 8

DEFAULT_BRANCH = "master"
RELEASE_BRANCH = "release/"
DEFAULT_REMOTE = "origin"


class ProcessPipe(threading.Thread):
    """Print output from processes"""
    # https://gist.github.com/alfredodeza/dcea71d5c0234c54d9b1

    def __init__(self, prefix=""):
        threading.Thread.__init__(self)
        self.daemon = False
        self.fdRead, self.fdWrite = os.pipe()
        self.pipeReader = os.fdopen(self.fdRead)
        self.process = None
        self.prefix=prefix
        self.start()

    def fileno(self):
        return self.fdWrite

    def run(self):
        try:
            for line in iter(self.pipeReader.readline, ''):
                print("{}{}".format(self.prefix, line.strip('\n')), flush=True)
        except:
            pass

        try:
            self.pipeReader.close()
        except:
            pass

    def close(self):
        try:
            os.close(self.fdWrite)
        except:
            pass

    def stop(self):
        self._stop = True
        self.close()

    def __del__(self):
        try:
            self.stop()
        except:
            pass

        try:
            del self.fdRead
            del self.fdWrite
        except:
            pass


class ProcessExe:
    """Execute a command"""

    def __init__(self, cmd, cwd=None, check=True):
        shell = False
        if platform.system() == "Linux":
            shell = True

        pipeout = ProcessPipe("OUT| ")
        pipeerr = ProcessPipe("ERR| ")
        try:
            process = subprocess.Popen(
                cmd, cwd=cwd,
                stdout=pipeout, stderr=pipeerr,
                universal_newlines=True, shell=shell
            )
        except:
            self.returncode = -1
            pipeout.close()
            pipeerr.close()
            raise

        # We must wait as sometimes when the process.wait() returns, it's not quite finished (file outputs are not yet
        # present), and the pipe for reading STDOUT and STDERR is not yet complete.
        self.returncode = process.wait()
        time.sleep(0.5)
        pipeout.close()
        pipeerr.close()
        time.sleep(0.5)

        if (check and self.returncode != 0):
            raise subprocess.CalledProcessError(
                returncode=process.returncode, cmd=cmd
            )

    @ staticmethod
    def run(cmd, cwd=None, check=True):
        """Run the command"""
        process = ProcessExe(cmd, cwd, check)
        return process


class GitExe:
    """Execute GIT commands"""

    def __init__(self, args, cwd=None, check=True):
        self.args = ["git"]
        for arg in args:
            self.args.append(arg)

        # Python 3.7
        #   process = subprocess.run(
        #       self.args, capture_output=True,
        #       text=True, shell=True, cwd=cwd
        #   )
        process = subprocess.run(
            self.args, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
            universal_newlines=True, shell=False, cwd=cwd
        )
        self.stdout = process.stdout.splitlines()
        self.stderr = process.stderr.splitlines()
        self.returncode = process.returncode

        if (process.returncode != 0):
            if (GITDEBUGLEVEL > 0):
                prcwd = os.path.relpath(cwd) if cwd != None else "."
                if (check):
                    print("GITERR: {} ({})".format(
                        " ".join(self.args), prcwd))
                    print(f"GITERR: Result={process.returncode}")
                    if (len(self.stdout) > 0):
                        for line in self.stdout:
                            print("STDOUT|", line)
                    if (len(self.stderr) > 0):
                        for line in self.stderr:
                            print("STDERR|", line)
                else:
                    print("GITCMD: {} (Result={}, {})".format(
                        " ".join(self.args), process.returncode, prcwd))
                    if (GITDEBUGLEVEL > 1):
                        if (len(self.stdout) > 0):
                            for line in self.stdout:
                                print("STDOUT|", line)
                        if (len(self.stderr) > 0):
                            for line in self.stderr:
                                print("STDERR|", line)
            if (check):
                raise subprocess.CalledProcessError(
                    returncode=process.returncode, cmd=self.args,
                    output=process.stdout, stderr=process.stderr
                )
        else:
            if (GITDEBUGLEVEL > 0):
                prcwd = os.path.relpath(cwd) if cwd != None else "."
                print("GITCMD: ", " ".join(self.args), end="")
                print(" ({})".format(prcwd))
                if (GITDEBUGLEVEL > 1):
                    if (len(self.stdout) > 0):
                        for line in self.stdout:
                            print("STDOUT|", line)
                    if (len(self.stderr) > 0):
                        for line in self.stderr:
                            print("STDERR|", line)

    _version = None

    @ staticmethod
    def run(args, cwd=None, check=True):
        """Run the command git <args>"""
        gitexe = GitExe(args, cwd, check)
        return gitexe

    @ classmethod
    def version(cls):
        """Get the current GIT version"""
        if (cls._version == None):
            gitexe = GitExe.run(["version"])
            cls._version = gitexe.stdout[0]
        return cls._version


class GitModule:
    """Perform operations on a GIT repository"""

    def __init__(self, relpath, cwd=None):
        self._relpath = relpath
        if (cwd == None):
            cwd = os.getcwd()
        self._path = os.path.realpath(os.path.join(cwd, relpath))

        self._toplevel = None
        self._username = None
        self._useremail = None

        self.default_branch = DEFAULT_BRANCH
        self.url = None

    def path(self):
        return self._relpath

    def printable_path(self, length):
        """Tries to split and shorten a path string within the length given"""
        if (len(self._relpath) <= length):
            return self._relpath

        def splitall(path):
            allparts = []
            while True:
                parts = os.path.split(path)
                if (parts[0] == path):
                    allparts.insert(0, parts[0])
                    break
                elif parts[1] == path:
                    allparts.insert(0, parts[1])
                    break
                else:
                    path = parts[0]
                    allparts.insert(0, parts[1])
            return allparts

        # We try to shorten each to the maximum length possible without
        # exceeding the total length from the beginning to the end.
        paths = splitall(self._relpath)
        strlen = len("/".join(paths))
        position = 0
        elements = len(paths)
        while (strlen > length and position < elements):
            element = paths[position]
            pathlen = len(element)
            if (pathlen > 3):
                excess = strlen - length - 1
                keep = pathlen - excess - 3
                if (keep < 1):
                    keep = 1
                paths[position] = element[0:keep] + ".."
                strlen += keep + 2 - pathlen
            position += 1
        return "/".join(paths)

    def top_level(self):
        """Get the top level folder path for this repository"""
        if (self._toplevel == None):
            try:
                git_top = GitExe.run(
                    ["rev-parse", "--show-toplevel"],
                    cwd=self._path
                )
                self._toplevel = os.path.realpath(git_top.stdout[0])
            except subprocess.CalledProcessError as ex:
                raise GitError(ex, errors=ex)

        return self._toplevel

    def get_git_user_name(self):
        """Get the configured user name for this repository"""
        if (self._username == None):
            try:
                git_config = GitExe.run(
                    ["config", "user.name"],
                    cwd=self.top_level()
                )
            except subprocess.CalledProcessError:
                return ""

            if (len(git_config.stdout) == 0):
                self._username = ""
            else:
                self._username = git_config.stdout[0]
        return self._username

    def get_git_user_email(self):
        """Get the configured user email for this repository"""
        if (self._useremail == None):
            try:
                git_config = GitExe.run(
                    ["config", "user.email"],
                    cwd=self.top_level()
                )
            except subprocess.CalledProcessError:
                return ""

            if (len(git_config.stdout) == 0):
                self._useremail = ""
            else:
                self._useremail = git_config.stdout[0]
        return self._useremail

    def set_git_user_name(self, username):
        try:
            GitExe.run(
                ["config", "user.name", username],
                cwd=self.top_level()
            )
        except subprocess.CalledProcessError as ex:
            raise GitError(ex, errors=ex)
        self._username = username

    def set_git_user_email(self, useremail):
        try:
            GitExe.run(
                ["config", "user.email", useremail],
                cwd=self.top_level()
            )
        except subprocess.CalledProcessError as ex:
            raise GitError(ex, errors=ex)
        self._useremail = useremail

    def set_config(self, rebase):
        exception = None

        try:
            if (platform.system() == "Windows"):
                GitExe.run(
                    ["config", "core.autocrlf", "true"],
                    cwd=self.top_level()
                )
            else:
                GitExe.run(
                    ["config", "core.autocrlf", "input"],
                    cwd=self.top_level()
                )
        except subprocess.CalledProcessError as ex:
            if (exception != None):
                exception = ex

        try:
            if (rebase):
                GitExe.run(
                    ["config", "pull.rebase", "true"],
                    cwd=self.top_level()
                )
            else:
                GitExe.run(
                    ["config", "pull.rebase", "false"],
                    cwd=self.top_level()
                )
        except subprocess.CalledProcessError as ex:
            if (exception != None):
                exception = ex

        try:
            GitExe.run(
                ["config", "remote.origin.prune", "true"],
                cwd=self.top_level()
            )
        except subprocess.CalledProcessError as ex:
            if (exception != None):
                exception = ex

        try:
            GitExe.run(
                ["config", "push.default", "simple"],
                cwd=self.top_level()
            )
        except subprocess.CalledProcessError as ex:
            if (exception != None):
                exception = ex

        if (exception != None):
            raise GitError(exception, errors=exception)

    def get_current_branch(self):
        git = GitExe.run(
            ["symbolic-ref", "-q", "--short", "HEAD"],
            cwd=self.top_level(), check=False
        )
        if (git.returncode == 0 and len(git.stdout) > 0):
            return git.stdout[0]
        return None

    def get_current_hash(self):
        try:
            git = GitExe.run(
                ["rev-parse", 'HEAD'],
                cwd=self.top_level()
            )
            return git.stdout[0]
        except subprocess.CalledProcessError:
            # We can get here if executed in an empty repository.
            return None

    def checkout_branch(self, branch=None, force=False):
        if (branch == None):
            if (self.default_branch == None):
                return
            branch = self.default_branch

        try:
            if (not force):
                current_branch = self.get_current_branch()
                if (current_branch == None or branch != current_branch):
                    GitExe.run(
                        ["checkout", branch],
                        cwd=self.top_level()
                    )
            else:
                GitExe.run(
                    ["checkout", "-f", branch],
                    cwd=self.top_level()
                )
        except subprocess.CalledProcessError as ex:
            raise GitError(ex, errors=ex)

    def get_ref_hash(self, gitref):
        """Get the hash for the given reference.

        If no reference is given, or it doesn't exist, then None is returned.
        """
        if (gitref == None):
            return None
        try:
            git = GitExe.run(
                ["rev-parse", "--verify", gitref],
                cwd=self.top_level()
            )
            return git.stdout[0]
        except subprocess.CalledProcessError:
            return None

    _RE_SHOWREF = re.compile(r'^([0-9a-fA-F]+)\s+(\S+)$')

    def get_ref_hashes(self, gitref=None):
        """Get all references for a branch/hash from heads and remotes.

        Returns a list of tuples in the form of (hash, ref), where hash and ref
        are strings.
        """

        try:
            if (gitref == None):
                git = GitExe.run(["show-ref"], cwd=self.top_level())
            else:
                git = GitExe.run(["show-ref", gitref], cwd=self.top_level())

            refs = []
            for entry in git.stdout:
                m = self._RE_SHOWREF.match(entry)
                if (m != None):
                    refhash = m.group(1)
                    refname = m.group(2)
                    refs.append((refhash, refname))
            return refs if len(refs) > 0 else None
        except subprocess.CalledProcessError:
            return None

    def get_branch_default_remote(self, branch):
        """Get the name of the default remote for the given branch"""
        if (branch == None):
            return None

        try:
            git = GitExe.run(
                ["config", "--local", f"branch.{branch}.remote"],
                cwd=self.top_level()
            )
            return git.stdout[0]
        except subprocess.CalledProcessError:
            return None

    def get_tracking_branch_from_head(self):
        try:
            git_ref = GitExe.run(
                ["symbolic-ref", "-q", "HEAD"],
                cwd=self.top_level()
            )
            ref = git_ref.stdout[0]
        except subprocess.CalledProcessError:
            return None

        try:
            git_remote_ref = GitExe.run(
                ["for-each-ref", "--format=%(upstream:short)", ref],
                cwd=self.top_level()
            )
            if (len(git_remote_ref.stdout) == 0):
                return None
            return git_remote_ref.stdout[0]
        except subprocess.CalledProcessError:
            return None

    def get_branches_remote_map(self):
        """Get all branches for local and remotes.

        Returns a dictionary where the key is a remote, and the value is a set
        of the references.
        """

        remotes = {}
        refs = self.get_ref_hashes()
        for ref in refs:
            if (ref[1].startswith("refs/heads/")):
                remote = None
                branch = ref[1][11:]
            elif (ref[1].startswith("refs/remotes/")):
                remoteref = ref[1][13:].split("/", 1)
                remote = remoteref[0]
                branch = remoteref[1]
            else:
                remote = None
                branch = None

            if (branch != None):
                if (not remote in remotes):
                    remotes[remote] = {branch}
                else:
                    remotes[remote].add(branch)

        return remotes

    _RE_BRANCH_REMOTE_KEY = re.compile(r'^branch\.(\S+)\.remote=(\S+)$')

    def get_default_remotes_map(self):
        """Get a list of all the default remotes for the local branches.

        Returns a dictionary, where the key is the branch, and the value is the
        default remote.
        """

        git_config = GitExe.run(
            ["config", "--local", "--list"],
            cwd=self.top_level()
        )
        branches = {}
        for line in git_config.stdout:
            m = self._RE_BRANCH_REMOTE_KEY.match(line)
            if (m != None):
                branch = m.group(1)
                remote = m.group(2)
                branches[branch] = remote
        return branches

    def get_is_dirty(self):
        git_dirty = GitExe.run(
            ["diff-index", "--quiet", "HEAD", "--", "."],
            cwd=self.top_level(), check=False
        )
        if (git_dirty.returncode != 0):
            return True
        return False

    def get_merge_base(self, current, base):
        if (current == None or base == None):
            return None

        try:
            git = GitExe.run(
                ["merge-base", current, base],
                cwd=self.top_level()
            )
            return git.stdout[0]
        except subprocess.CalledProcessError:
            return None

    def get_count_commits(self, current, base):
        if (current == None or base == None):
            return None

        try:
            git = GitExe.run(
                ["rev-list", "--count", current, f"^{base}"],
                cwd=self.top_level()
            )
            return int(git.stdout[0])
        except subprocess.CalledProcessError:
            return None

    def pull(self, ffonly=False, force=False, recurse=True):
        args = ["pull"]
        if (ffonly):
            args.append("--ff-only")
        if (force):
            args.append("--force")
        if (recurse):
            args.append("--recurse-submodules")
        else:
            args.append("--no-recurse-submodules")

        try:
            GitExe.run(args, cwd=self.top_level())
        except subprocess.CalledProcessError as ex:
            raise GitError(ex, errors=ex)

    def fetch(self, force=False, recurse=True):
        args = ["fetch", "--all", "--prune"]
        if (force):
            args.append("--force")
        if (recurse):
            args.append("--recurse-submodules")
        else:
            args.append("--no-recurse-submodules")

        try:
            GitExe.run(args, cwd=self.top_level())
        except subprocess.CalledProcessError as ex:
            raise GitError(ex, errors=ex)

    def clean(self):
        try:
            GitExe.run(
                ["clean", "-xfd"],
                cwd=self.top_level()
            )
        except subprocess.CalledProcessError as ex:
            raise GitError(ex, errors=ex)

    def delete_branch(self, branch, remote=None):
        if (branch == None):
            return

        try:
            if (remote == None):
                GitExe.run(
                    ["branch", "-D", branch],
                    cwd=self.top_level()
                )
            else:
                GitExe.run(
                    ["push", remote, f":{branch}"],
                    cwd=self.top_level()
                )
        except subprocess.CalledProcessError as ex:
            raise GitError(ex, errors=ex)


class GitSubModule:
    """Record for keeping information about a submodule"""

    def __init__(self):
        self.path = None
        self.uri = None
        self.branch = None


class GitModules:
    """maintain a list of git submodules"""

    def __init__(self):
        self._path = os.getcwd()
        self._toplevel = None
        self._modules = None

    def top_level(self):
        """Get the top level directory for the super project"""

        top_super = None
        if (self._toplevel == None):
            try:
                git_super = GitExe.run(
                    ["rev-parse", "--show-superproject-working-tree"],
                    cwd=self._path
                )
                if (len(git_super.stdout) > 0):
                    top_super = git_super.stdout[0]
            except subprocess.CalledProcessError:
                # Either the command isn't supported, or we're not in a GIT
                # repo.
                pass

            if (top_super == None):
                try:
                    git_top = GitExe.run(
                        ["rev-parse", "--show-toplevel"],
                        cwd=self._path
                    )
                    self._toplevel = os.path.realpath(git_top.stdout[0])
                except subprocess.CalledProcessError:
                    self._toplevel = None
            else:
                self._toplevel = \
                    os.path.realpath(top_super) if top_super != None else None

        return self._toplevel

    def at_base(self):
        """Check if we're at the top level git repository, and not in some submodule"""
        git_toplevel = self.top_level()
        if (git_toplevel == None):
            return False

        git_cwd = os.getcwd()
        return (git_toplevel == git_cwd)

    _RE_SUBMODULE_KEY = re.compile(r'submodule\.(\S+)\.(\S+)=(.+)')

    def get_submodules(self):
        """Return a list of GitModule objects for each submodule at the current superproject."""

        configfile = os.path.join(self.top_level(), ".gitmodules")
        if (not os.path.isfile(configfile)):
            return []

        if (self._modules == None):
            git_modules = GitExe.run(
                ["config", "--file", ".gitmodules", "--list"],
                cwd=self.top_level()
            )
            module_configs = {}
            for line in git_modules.stdout:
                m = self._RE_SUBMODULE_KEY.match(line)
                if (m != None):
                    mk = m.group(1)
                    if (mk in module_configs):
                        module_config = module_configs[mk]
                    else:
                        module_config = GitSubModule()
                        module_configs[mk] = module_config
                    mp = m.group(2)
                    mv = m.group(3)
                    if (mp == "path"):
                        module_configs[mk].path = mv
                    elif (mp == "branch"):
                        module_configs[mk].branch = mv
                    elif (mp == "url"):
                        module_configs[mk].uri = mv

            modules = []
            for module in sorted(module_configs):
                git_module = GitModule(
                    module_configs[module].path, cwd=self.top_level())
                git_module.default_branch = module_configs[module].branch
                git_module.url = module_configs[module].uri
                modules.append(git_module)
            self._modules = modules

        return self._modules

    def git_submodules_init(self, force=False):
        cmd = ["submodule", "update", "--init", "--jobs", "8"]
        if (force):
            cmd.append("--force")
        try:
            GitExe.run(cmd, cwd=self.top_level())
        except subprocess.CalledProcessError as ex:
            raise GitError(ex, errors=ex)


class EnvironmentError(Exception):
    """Exception: The script cannot run, due to missing dependencies or wrong run-time environment."""

    def __init__(self, message, errors=None):
        super(EnvironmentError, self).__init__(message)
        self.errors = errors


class ArgumentError(Exception):
    """Exception: There's an error on the command line, and it can't be parsed."""

    def __init__(self, message, errors=None):
        super(ArgumentError, self).__init__(message)
        self.errors = errors


class CommandError(Exception):
    """Exception: There was an error executing the command."""

    def __init__(self, message, exitcode=-1, errors=None):
        super(CommandError, self).__init__(message)
        self.errors = errors
        self.exitcode = exitcode


class GitError(Exception):
    """Exception: Error executing a GIT command"""

    def __init__(self, message, errors=None):
        super(GitError, self).__init__(message)
        self.error = errors
        self.message = message

    def __str__(self):
        # Make the error code more readable. Instead of something like:
        #  Command '['cmd', 'arg', 'arg']' returned non-zero exit status 1.
        #
        # make it more like
        #  Command 'cmd arg arg' returned 1
        if (type(self.error) is subprocess.CalledProcessError):
            message = "Command '{}' returned {}.".format(
                ' '.join(self.error.cmd), self.error.returncode)
            if (len(self.error.stdout) > 0):
                message += "\nSTDOUT: {}".format(self.error.stdout.rstrip())
            if (len(self.error.stderr) > 0):
                message += "\nSTDERR: {}".format(self.error.stderr.rstrip())
            return message
        if (not (self.error is None)):
            return str(self.error)
        return str(self.message)


class Expansion:
    """Handle expansion blocks"""

    def __init__(self, expansion, defexpansion):
        self.expansion = expansion
        self.defexpansion = defexpansion

    def expand(self, expstring):
        # Parse the expstring. If we find ${xxx} where 'xxx' is in the range
        # a-z, A-Z, and then '(', ')', _, 0-9, this is considered an expansion
        # variable. If the $ is escaped with \$ (in JSON, this needs to be \\$),
        # we treat the next text literally.
        #
        # A dictionary of expansion variables is maintained when expanding, so
        # we can abort if the expansion variables become recursive, forming a
        # cyclic graph.

        def _validchar(symbolpos, char) -> bool:
            if (char >= 'a' and char <= 'z') or (char >= 'A' and char <= 'Z'):
                return True

            if symbolpos == 0:
                return False

            if (char >= '0' and char <= '9') or char == '_' or char == '(' or char == ')':
                return True

            return False

        def _envLookup(symbol):
            if self.expansion != None and "env" in self.expansion:
                if symbol in self.expansion["env"]:
                    return self.expansion["env"][symbol]

            if self.defexpansion != None and "env" in self.defexpansion:
                if symbol in self.defexpansion["env"]:
                    return self.defexpansion["env"][symbol]

            return None

        def _toolLookup(symbol):
            block = None
            if self.expansion != None and "tools" in self.expansion:
                if symbol in self.expansion["tools"]:
                    block = self.expansion["tools"][symbol]

            if self.defexpansion != None and "tools" in self.defexpansion:
                if symbol in self.defexpansion["tools"]:
                    block = self.defexpansion["tools"][symbol]

            if block == None:
                raise CommandError(f"No tools defined for '{symbol}'")

            if not "exe" in block:
                raise CommandError(f"No exe defined for tool '{symbol}'")
            exename = block["exe"]

            machine = platform.machine()
            if machine in block:
                archblock = block[machine]
            elif "any" in block:
                archblock = block["any"]
            else:
                raise CommandError(f"No valid machine '{machine}' defined for tool '{symbol}'")

            if not "path" in archblock:
                raise CommandError(f"No paths for machine '{machine}' defined for tool '{symbol}'")

            for path in archblock["path"]:
                localfound = dict()
                exppath = _parse(localfound, path)
                if exppath != None:
                    testpath = f"{exppath}{exename}"
                    if os.path.isfile(testpath):
                        return f"\"{testpath}\""

            return exename

        def _parse(symbolsfound, expstring):
            symbolstart = None
            resultstring = ""
            charpos = 0

            for c in expstring:
                if symbolstart == None:
                    if c == '$':
                        symbolstart = charpos
                    else:
                        resultstring = resultstring + c
                else:
                    symbollen = charpos - symbolstart
                    if symbollen == 1:
                        if c != '{':
                            # Expect to start with ${
                            resultstring = resultstring + expstring[symbolstart:(charpos + 1)]
                            symbolstart = None
                    elif c == '}':
                        # We've got ${...}
                        if symbollen == 2:
                            # There is no 'variable', so it's invalid
                            resultstring = resultstring + expstring[symbolstart:(charpos + 1)]
                            symbolstart = None
                            raise CommandError(f"Empty expansion in '{expstring}'")
                        else:
                            variable = expstring[(symbolstart + 2):charpos]
                            if variable in symbolsfound:
                                symbolstart = None
                                raise CommandError(f"Recursive variable expansion found for '{variable}'")

                            symbolstart = None

                            var = os.getenv(variable)
                            if (var != None):
                                # We don't expand environment strings further
                                resultstring = resultstring + var
                            else:
                                # Assume this variable must be expanded
                                symbolsfound[variable] = True

                                # Simple substitution
                                newexpstring = _envLookup(variable)
                                if (newexpstring != None):
                                    resultstring = resultstring + _parse(symbolsfound, newexpstring)
                                else:
                                    # Or tool lookup
                                    newexpstring = _toolLookup(variable)
                                    if newexpstring != None:
                                        resultstring = resultstring + newexpstring

                    elif not _validchar(charpos - symbolstart + 2, c):
                        # The variable has invalid characters
                        resultstring = resultstring + expstring[symbolstart:(charpos + 1)]
                        symbolstart = None

                charpos = charpos + 1

            return resultstring

        found = dict()
        return _parse(found, expstring)


class VersionCommand:
    """Parse the 'version' command"""

    def __init__(self, arguments):
        self.argparser = argparse.ArgumentParser(
            prog="git rj version",
            description="Print the version of this script, and the Python interpreter in use")
        self.arguments = self.argparser.parse_args(arguments)

    def execute(self):
        print("git rj")
        print("  Version: {}".format(VERSION))
        print(
            "  Python: {}.{}.{}-{}"
            .format(
                sys.version_info.major,
                sys.version_info.minor, sys.version_info.micro, sys.version_info.releaselevel
            )
        )
        print("  GIT: {}".format(GitExe.version()))


class HelpCommand:
    """Provides some very basic help information"""

    def __init__(self, arguments):
        pass

    def execute(self):
        vc = VersionCommand([])
        vc.execute()

        print()
        print("The following basic commands are provided:")
        print("  git rj version - Show the version")
        print("  git rj help - Print this help")
        print("  git rj init - Initialize the repositories")
        print("  git rj pull - Pull all modules")
        print("  git rj fetch - Fetch all modules")
        print("  git rj clean - clean all modules")
        print("  git rj status - Get repository information and status")
        print("  git rj cobr - Check out a branch")
        print("  git rj shbr - Show branches")
        print("  git rj rmbr - Remove branches")
        print("  git rj build - Build from .gitrjbuild description")
        print("  git rj perf - Run performance tests with BenchmarkDotNet")
        print()
        print("Get information about the command with the -h option, e.g.")
        print("  git rj status -h")


class InitCommand:
    """Parse the 'init' command arguments on the command line.

    It will not modify change the current branch for the base repository, only
    checking out the submodules, configuring them and pulling them. You should
    manually check out the correct branch of the base repository first.
    """

    def __init__(self, arguments):
        argparser = argparse.ArgumentParser(
            prog="git rj init",
            description="Initialize the modules for usage. This command, when given with "
            "no options, will initialize submodules (with git submodule updat --init), "
            "apply the top level configuration on all repositories (taking your current "
            "user name and email address), and check out to the default branch for each "
            "submodule. It will then do a fast-forward only pull (with a force fetch "
            "update)")
        argparser.add_argument(
            "-i", "--init", action="store_true",
            help="Initialize the submodules. This will check out the exact commit.")
        argparser.add_argument(
            "-c", "--config", action="store_true",
            help="Update the git configuration for the base module and all submodules.")
        argparser.add_argument(
            "-b", "--checkout", action="store_true",
            help="Check out the default branch, as given in the .gitmodules file.")
        argparser.add_argument(
            "-p", "--pull", action="store_true",
            help="Pull after the default branch is checked out, using fast-forward only, "
            "and force updating the refs with the fetch.")
        argparser.add_argument(
            "-f", "--force", action="store_true",
            help="Applies the force option, which at this time causes a forced checkout, "
            "overwriting changes when checking out the branch.")

        self.arguments = argparser.parse_args(arguments)

        # If no options are given, then we do all actions
        if (not (self.arguments.init or self.arguments.config
                 or self.arguments.checkout or self.arguments.pull)):
            self.arguments.init = True
            self.arguments.config = True
            self.arguments.checkout = True
            self.arguments.pull = True

    def execute(self):
        modules = GitModules()
        if (not modules.at_base()):
            raise CommandError("Not at the top level repository.")

        base_module = GitModule(modules.top_level())
        if (self.arguments.config):
            git_username = base_module.get_git_user_name()
            git_email = base_module.get_git_user_email()
            if (git_username == "" or git_email == ""):
                raise CommandError(
                    "Please set the user name and email in the super project top level repository."
                )

        print("\033[35;1mInitialising:\033[0;35m base\033[0m")
        if (self.arguments.config):
            print("  Using {} <{}>".format(git_username, git_email))
            print("  Setting Config... ", end="", flush=True)
            base_module.set_config(False)
            print("DONE.", flush=True)
        if (self.arguments.init):
            print("  Submodule Init... ", end="", flush=True)
            try:
                modules.git_submodules_init(force=self.arguments.force)
                print("DONE.", flush=True)
            except GitError as ex:
                print("FAILED.\n{}".format(str(ex)), flush=True)
                return

        execute_lock = threading.Lock()

        def _execute(module):
            op = False
            try:
                if (self.arguments.config):
                    module.set_git_user_name(git_username)
                    module.set_git_user_email(git_email)
                    module.set_config(True)
                op = True
                if (self.arguments.checkout):
                    module.checkout_branch(force=self.arguments.force)
                    op = True
                if (self.arguments.pull):
                    module.pull(ffonly=True, force=True)
                    op = True
            except GitError as ex:
                with execute_lock:
                    print("\033[35;1mModule:\033[0;35m {}...\033[0m FAILED.\n{}"
                          .format(module.path(), str(ex)), flush=True)
                return
            if (op):
                with execute_lock:
                    print("\033[35;1mModule:\033[0;35m {}...\033[0m DONE."
                          .format(module.path()), flush=True)

        # Run the initialization on submodules in parallel
        with concurrent.futures.ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
            for module in modules.get_submodules():
                executor.submit(_execute, module)


class PullCommand:
    """Parse the 'pull' command arguments on the command line."""

    def __init__(self, arguments):
        argparser = argparse.ArgumentParser(
            prog="git rj pull",
            description="Pulls the base repository and all submodules.")
        argparser.add_argument(
            "-f", "--force", action="store_true",
            help="Discards local changes before pulling.")

        self.arguments = argparser.parse_args(arguments)

    def execute(self):
        modules = GitModules()
        if (not modules.at_base()):
            raise CommandError("Not at the top level repository.")

        execute_lock = threading.Lock()

        def _execute(module, name=None, recurse=True, force=False):
            if (name == None):
                name = module.path()
            try:
                remote = module.get_tracking_branch_from_head()
                if (remote == None):
                    raise GitError("Not on a tracking branch, can't pull")

                if (not force):
                    # Just do a normal pull. If the command fails, we'll
                    # report the error.
                    module.pull(recurse=recurse)
                else:
                    branch = module.get_current_branch()
                    if (branch == None):
                        raise GitError("No branch to pull / reset to")
                    module.checkout_branch(branch=branch, force=True)
                    module.pull(recurse=recurse)

                with execute_lock:
                    print(f"\033[35;1mModule:\033[0;35m {name}...\033[0m DONE.",
                          flush=True)
            except GitError as ex:
                with execute_lock:
                    print("\033[35;1mModule:\033[0;35m {}...\033[0m FAILED.\n{}"
                          .format(name, str(ex)), flush=True)

        base_module = GitModule(modules.top_level())
        _execute(base_module, name="base", recurse=False,
                 force=self.arguments.force)

        # Run the initialization on submodules in parallel
        with concurrent.futures.ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
            for module in modules.get_submodules():
                executor.submit(_execute, module, force=self.arguments.force)


class FetchCommand:
    """Fetches for all repositories"""

    def __init__(self, arguments):
        argparser = argparse.ArgumentParser(
            prog="git rj fetch",
            description="Fetches from the remotes for the base repository and all submodules.")
        argparser.add_argument(
            "-f", "--force", action="store_true",
            help="Forces the fetch update by passing --force to git fetch.")

        self.arguments = argparser.parse_args(arguments)

    def execute(self):
        modules = GitModules()
        if (not modules.at_base()):
            raise CommandError("Not at the top level repository.")

        execute_lock = threading.Lock()

        def _execute(module, name=None, force=False, recurse=True):
            if (name == None):
                name = module.path()
            try:
                module.fetch(force=force, recurse=recurse)
                with execute_lock:
                    print(f"\033[35;1mModule:\033[0;35m {name}...\033[0m DONE.",
                          flush=True)
            except GitError as ex:
                with execute_lock:
                    print("\033[35;1mModule:\033[0;35m {}...\033[0m FAILED.\n{}"
                          .format(name, str(ex)), flush=True)

        base_module = GitModule(modules.top_level())
        _execute(base_module, name="base",
                 force=self.arguments.force, recurse=False)

        # Run the initialization on submodules in parallel
        with concurrent.futures.ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
            for module in modules.get_submodules():
                executor.submit(_execute, module, force=self.arguments.force)


class CleanCommand:
    """Cleans all the repositories"""

    def __init__(self, arguments):
        argparser = argparse.ArgumentParser(
            prog="git rj clean",
            description="Cleans the repositories, often used to ensure a clean build.")
        argparser.add_argument(
            "-a", "--all", action="store_true",
            help="Also cleans the base repository, by default it cleans only submodules.")

        self.arguments = argparser.parse_args(arguments)

    def execute(self):
        modules = GitModules()
        if (not modules.at_base()):
            raise CommandError("Not at the top level repository.")

        execute_lock = threading.Lock()

        def _execute(module, name=None):
            if (name == None):
                name = module.path()
            try:
                module.clean()
                with execute_lock:
                    print(f"\033[35;1mModule:\033[0;35m {name}...\033[0m DONE.",
                          flush=True)
            except GitError as ex:
                with execute_lock:
                    print("\033[35;1mModule:\033[0;35m {}...\033[0m FAILED.\n{}"
                          .format(name, str(ex)), flush=True)

        base_module = GitModule(modules.top_level())
        if (self.arguments.all):
            _execute(base_module, name="base")

        # Run the initialization on submodules in parallel
        with concurrent.futures.ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
            for module in modules.get_submodules():
                executor.submit(_execute, module)


class StatusCommand:
    """Gets the status for all modules"""

    def __init__(self, arguments):
        argparser = argparse.ArgumentParser(
            prog="git rj status",
            description="Queries all the modules for their status.")
        argparser.add_argument(
            "-l", "--long", action="store_true",
            help="Shows long output for more clarity.")

        self.arguments = argparser.parse_args(arguments)

    def execute(self):
        modules = GitModules()
        if (not modules.at_base()):
            raise CommandError("Not at the top level repository.")

        execute_lock = threading.Lock()

        def _get_current_branch(module):
            current_branch = module.get_current_branch()
            current_hash = module.get_current_hash()
            if (current_branch == None and current_hash == None):
                return None

            if (current_hash == None):
                current_hash = "0000000000000000000000000000000000000000"
            return (current_hash, current_branch)

        def _get_destination_branch(module):
            if (module.default_branch == None):
                return None

            default_refs = module.get_ref_hashes(module.default_branch)
            if (default_refs == None):
                return None

            # Gets the local branch's tracking remote. If there's no local
            # branch, then this is None.
            default_remote = module.get_branch_default_remote(
                module.default_branch)

            # look for 'refs/remotes/{default_remote}/{default_branch}' first,
            # followed by 'refs/heads/{default_branch}' and return the branch
            # name and the hash. This is the commit we refer to when calculating
            # how far we've branched (i.e. refer first to the default remote).
            local = None
            remote = None
            multiple = False
            origin_found = False
            for ref in default_refs:
                if (ref[1] == f"refs/heads/{module.default_branch}"):
                    local = (ref[0], module.default_branch)
                else:
                    if (default_remote != None):
                        if (ref[1] == f"refs/remotes/{default_remote}/{module.default_branch}"):
                            remote = (ref[0],
                                      f"{default_remote}/{module.default_branch}")
                    else:
                        if (ref[1].startswith("refs/remotes/")):
                            if (remote == None):
                                # Take the first remote always.
                                if (ref[1].startswith(f"refs/remotes/{DEFAULT_REMOTE}/")):
                                    origin_found = True
                                remote = (ref[0], ref[1][13:])
                            else:
                                # If we find more than one remote, then only
                                # take it if it's from DEFAULT_REMOTE (usually
                                # 'origin')
                                multiple = True
                                if (ref[1].startswith(f"refs/remotes/{DEFAULT_REMOTE}/")):
                                    remote = (ref[0], ref[1][13:])
                                    origin_found = True
            if (multiple and not origin_found):
                remote = None

            # See section 2.5.7 of the README.gitrj.md
            if (local == None):
                return remote
            if (default_remote == None or remote == None):
                return local
            return remote

        def _execute(module, name=None):
            hash_len = 0 if self.arguments.long else 11
            module_len = 40 if self.arguments.long else 30

            try:
                # The current branch is only None if we're in an empty
                # repository
                current = _get_current_branch(module)
                isdirty = module.get_is_dirty() \
                    if current != None else None
                remote = _get_destination_branch(module)
                tracking = module.get_tracking_branch_from_head()
                if (remote == None):
                    mergebase = None
                    commits = None
                    behind = None
                    rebase = False
                else:
                    mergebase = module.get_merge_base(current[0], remote[0])
                    commits = module.get_count_commits(current[0], mergebase)
                    behind = module.get_count_commits(remote[0], mergebase)
                    rebase = not (mergebase == remote[0])

                out_name = name \
                    if name != None \
                    else module.printable_path(module_len)
                out_hash = current[0] \
                    if self.arguments.long \
                    else current[0][:hash_len]

                push_required = False
                trackcode = "-"
                if (tracking):
                    tracking_hash = module.get_ref_hash(tracking)
                    if (tracking_hash == None):
                        # Current branch is being tracked, but remote doesn't exist
                        trackcode = "t"
                    else:
                        # Current branch is being tracked
                        trackcode = "T"
                        if (tracking_hash != current[0]):
                            push_required = True

                if (push_required):
                    localmergebase = \
                        module.get_merge_base(current[0], tracking_hash)
                    localcommits = \
                        module.get_count_commits(current[0], localmergebase)
                    localbehind = \
                        module.get_count_commits(tracking_hash, localmergebase)

                with execute_lock:
                    print("[{}{}{}{}] {:<{}} {} (commits: {} / {}) [{} -> {}]"
                          .format(
                              "M" if isdirty else "-",
                              trackcode,
                              "P" if push_required else "-",
                              "R" if rebase else "-",
                              out_name, module_len,
                              out_hash,
                              commits if commits != None else "-",
                              behind if behind != None else "-",
                              current[1] if current != None else "-",
                              remote[1] if remote != None else module.default_branch
                          ), flush=True)
                    if (push_required):
                        print("   Local Branch: {} (by {} commit{})".format(
                            current[1], localcommits, "s" if localcommits > 0 else ""
                        ))
                        print("   Tracking Branch: {} (by {} commit{})".format(
                            tracking, localbehind, "s" if localbehind > 0 else ""
                        ))
            except GitError as ex:
                with execute_lock:
                    print("\033[35;1mModule:\033[0;35m {}...\033[0m FAILED.\n{}"
                          .format(name, str(ex)), flush=True)

        base_module = GitModule(modules.top_level())
        _execute(base_module, name="base")

        # Run the initialization on submodules in parallel
        with concurrent.futures.ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
            for module in modules.get_submodules():
                executor.submit(_execute, module)


class CobrCommand:
    """Check out a branch for all repositories"""

    def __init__(self, arguments):
        argparser = argparse.ArgumentParser(
            prog="git rj cobr",
            description="Checks out a specific branch for all repositories, or the default branch.")
        argparser.add_argument(
            "branch", nargs='?', default=None,
            help="The branch to check out.")
        argparser.add_argument(
            "-f", "--force", action="store_true",
            help="Discard all local changes.")
        argparser.add_argument(
            "-p", "--pull", action="store_true",
            help="Pull the local branch once checked out."
        )

        self.arguments = argparser.parse_args(arguments)

    def execute(self):
        modules = GitModules()
        if (not modules.at_base()):
            raise CommandError("Not at the top level repository.")

        execute_lock = threading.Lock()

        def _execute(module, name=None, default=None, recurse=True):
            if (name == None):
                name = module.path()

            # No force:
            #   Check out branch (force) if not None and it exists, else
            #   Check out default (force) if not None and it exists, else
            #   do nothing.
            actual = None

            current_branch = module.get_current_branch()
            if (self.arguments.branch != None):
                try:
                    if (self.arguments.force or current_branch != self.arguments.branch):
                        module.checkout_branch(
                            branch=self.arguments.branch,
                            force=self.arguments.force)
                    actual = self.arguments.branch
                except GitError:
                    pass

            if (actual == None and default != None):
                try:
                    if (self.arguments.force or current_branch != default):
                        module.checkout_branch(
                            branch=default,
                            force=self.arguments.force)
                    actual = default
                except GitError:
                    pass

                if (actual == None and DEFAULT_BRANCH != None):
                    try:
                        if (self.arguments.force or current_branch != DEFAULT_BRANCH):
                            module.checkout_branch(
                                branch=DEFAULT_BRANCH,
                                force=self.arguments.force)
                        actual = DEFAULT_BRANCH
                    except GitError:
                        pass

            try:
                if (self.arguments.pull):
                    module.pull(force=self.arguments.force, recurse=recurse)
            except GitError as ex:
                with execute_lock:
                    pbranch = actual if actual != None else current_branch
                    print("\033[35;1mModule:\033[0;35m {}...\033[0m Pull FAILED. {}\n{}"
                          .format(name, pbranch, str(ex)), flush=True)
                return

            with execute_lock:
                if (actual == None):
                    print(f"\033[35;1mModule:\033[0;35m {name}...\033[0m NONE.",
                          flush=True)
                else:
                    print(f"\033[35;1mModule:\033[0;35m {name}...\033[0m DONE. {actual}",
                          flush=True)

        base_module = GitModule(modules.top_level())

        # Check out the base repository, only if a branch is given. If none is
        # given, then use the current branch, and check out all submodules
        # dependend on the default branch for each submodule
        if (self.arguments.branch != None):
            _execute(base_module, name="base", default=None, recurse=False)

        with concurrent.futures.ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
            for module in modules.get_submodules():
                executor.submit(_execute, module,
                                default=module.default_branch)


class ShbrCommand:
    """Show all branches for all remotes for the repositories"""

    def __init__(self, arguments):
        argparser = argparse.ArgumentParser(
            prog="git rj shbr",
            description="Lists all the branches locally and those fetched from remotes. "
            "Additonally, for the local branches, if the same branch exists on a remote, "
            "it is printed again."
        )
        argparser.add_argument(
            "-r", "--show-release", action="store_true",
            help=f"Show in addition branches beginning with '{RELEASE_BRANCH}'.")

        self.arguments = argparser.parse_args(arguments)

    def execute(self):
        modules = GitModules()
        if (not modules.at_base()):
            raise CommandError("Not at the top level repository.")

        # A dictionary of remotes, where the key is the remote (None for local),
        # whose value is a set of the branches that are present. Also the
        # equivalent of a dictionary of branches which exist on remotes (not
        # including the local repository).
        execute_lock = threading.Lock()
        remotes = {}
        branches = {}
        error = False

        def _get_remotes(module):
            nonlocal error
            try:
                refs = module.get_ref_hashes()
            except GitError:
                error = True
                return

            # Take the list of hashes, the tuple element 1 is the ref. Check the
            # ref against strings to get the remotes.
            for ref in refs:
                if (ref[1].startswith("refs/heads/")):
                    remote = None
                    branch = ref[1][11:]
                elif (ref[1].startswith("refs/remotes/")):
                    # split the remote from the branch
                    remoteref = ref[1][13:].split("/", 1)
                    remote = remoteref[0]
                    branch = remoteref[1]
                else:
                    remote = None
                    branch = None

                if (not self.arguments.show_release):
                    if (branch != None and branch.startswith(RELEASE_BRANCH)):
                        remote = None
                        branch = None

                with execute_lock:
                    if (branch != None):
                        if (not remote in remotes):
                            remotes[remote] = {branch}
                        else:
                            remotes[remote].add(branch)

                        if (remote != None):
                            if (not branch in branches):
                                branches[branch] = {remote}
                            else:
                                branches[branch].add(remote)

        def _print_remotes(remote):
            if (not remote in remotes):
                return

            if (remote == None):
                print("Local:")
                for branch in sorted(remotes[None]):
                    if (not branch in branches):
                        print("  {}".format(branch))
                    else:
                        print("  {} (\033[32m{}\033[0m)".format(
                            branch, " ".join(sorted(branches[branch]))))
            else:
                print("Remote:", remote)
                for branch in sorted(remotes[remote]):
                    print(f"  {branch}")

        base_module = GitModule(modules.top_level())

        with concurrent.futures.ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
            check_modules = [base_module]
            check_modules.extend(modules.get_submodules())
            for module in check_modules:
                executor.submit(_get_remotes, module)

        if (error):
            print("An error was seen getting remotes...")

        print_remotes = []
        _print_remotes(None)
        for remote in remotes:
            if (remote != None):
                if (remote == DEFAULT_REMOTE):
                    _print_remotes(DEFAULT_REMOTE)
                else:
                    print_remotes.append(remote)
        for remote in sorted(print_remotes):
            _print_remotes(remote)


class RmbrCommand:
    """Remove branches from all repositories"""

    def __init__(self, arguments):
        argparser = argparse.ArgumentParser(
            prog="git rj rmbr",
            description="Delete branches from repositories.")
        argparser.add_argument(
            "-p", "--prune", action="store_true",
            help="Remove all orphaned local branches that have a tracking "
            "branch which no longer exists. Useful for pruning outdated branches "
            "after remote references were pruned from a previous fetch. If no branch "
            "name is given, then prune all branches, else only those listed if they "
            "are orphaned.")
        argparser.add_argument(
            "-l", "--local", action="store_true",
            help="Remove branches locally. This is the default. At least one branch "
            "must be given on the command line.")
        argparser.add_argument(
            "-r", "--remote", action="store_true",
            help="Remove remote branches. Also specify --local to remove "
            "all branches. At least one branch must be given on the command line.")
        argparser.add_argument(
            "branch", nargs="*",
            help="The name of the branches to remove.")

        self.arguments = argparser.parse_args(arguments)
        if (not self.arguments.local and not self.arguments.remote and not self.arguments.prune):
            self.arguments.local = True

        if ((self.arguments.local or self.arguments.remote)
                and (self.arguments.branch == None or len(self.arguments.branch) == 0)):
            raise ArgumentError("Must specify at least one branch when "
                                "using option --local or --remote")

    def execute(self):
        modules = GitModules()
        if (not modules.at_base()):
            raise CommandError("Not at the top level repository.")

        # TODO: Combine prune and remove into a single method, so that we print
        # fewer lines.

        execute_lock = threading.Lock()

        def _execute(module, local, remote, prune, name=None, branches=None):
            if (name == None):
                name = module.path()

            # To prune, we get a list of all the branches locally. If the branch
            # has a remote tracking reference (through the config entry
            # "branch.{branch}.remote"), then check if that remote exists. If it
            # doesn't, then we remove that branch locally if it's not the
            # default, and if it's not the current branch.
            #
            # This means, branches that were tracking (they have a default
            # remote), but where that remote ref isn't currently present, are
            # assumed to have been pruned externally. If you makea branch, but
            # there is no tracking, then it won't be pruned (so we don't delete
            # branches that haven't been pushed yet)

            remote_refs = module.get_branches_remote_map()
            branch_default_remote = module.get_default_remotes_map()
            current_branch = module.get_current_branch()
            default_branch = module.default_branch
            safe_branches = \
                [current_branch, default_branch, "HEAD", "FETCH_HEAD"]

            # Dictionary of remotes to a set of branches
            to_delete = {}

            def _add_to_delete_check(remote, branch):
                if (branch in remote_refs[remote] and not branch in safe_branches):
                    _add_to_delete(remote, branch)

            def _add_to_delete(remote, branch):
                if (not remote in to_delete):
                    to_delete[remote] = {branch}
                else:
                    to_delete[remote].add(branch)

            for remote_ref in remote_refs:
                if (remote_ref == None):
                    if (local):
                        for branch in branches:
                            _add_to_delete_check(remote_ref, branch)
                    if (prune):
                        # Allow -rp to remove all branches that are orphaned
                        prune_branches = branches if not remote else None
                        for branch in remote_refs[None]:
                            if (branch in branch_default_remote):
                                default_remote = branch_default_remote[branch]
                                if ((not default_remote in remote_refs or
                                     not branch in remote_refs[default_remote]) and
                                        not branch in safe_branches):
                                    if (prune_branches == None or branch in prune_branches):
                                        _add_to_delete(remote_ref, branch)
                else:
                    if (remote):
                        for branch in branches:
                            _add_to_delete_check(remote_ref, branch)
                            if (prune):
                                # Allow -rp to remove the now orphaned branch
                                _add_to_delete_check(None, branch)

            deleted_failed = {}
            deleted_success = {}
            if (len(to_delete) > 0):
                for remote in to_delete:
                    for branch in to_delete[remote]:
                        try:
                            module.delete_branch(branch, remote)
                        except GitError as ex:
                            if (not remote in deleted_failed):
                                deleted_failed[remote] = {branch: ex}
                            else:
                                deleted_failed[remote].add((branch, ex))
                        else:
                            if (not remote in deleted_success):
                                deleted_success[remote] = {branch}
                            else:
                                deleted_success[remote].add(branch)

            with execute_lock:
                if (len(deleted_failed) > 0):
                    print("\033[35;1mModule:\033[0;35m {}...\033[0m FAILED:"
                          .format(name), flush=True)
                    for remote in deleted_failed:
                        for branch in deleted_failed[remote]:
                            print("  {}: {}"
                                  .format(branch, str(deleted_failed[remote][branch])))
                if (len(deleted_success) > 0):
                    print("\033[35;1mModule:\033[0;35m {}...\033[0m Deleted:"
                          .format(name), flush=True)
                    for remote in deleted_success:
                        branches = " ".join(deleted_success[remote])
                        if (remote == None):
                            print(f"  (local) => {branches}")
                        else:
                            print(f"  {remote} => {branches}")

        base_module = GitModule(modules.top_level())
        check_modules = [(base_module, "base")]
        for module in modules.get_submodules():
            check_modules.extend([(module, None)])

        with concurrent.futures.ThreadPoolExecutor(max_workers=MAX_WORKERS) as executor:
            for module in check_modules:
                branches = self.arguments.branch \
                    if len(self.arguments.branch) > 0 else None
                executor.submit(_execute, module[0],
                                self.arguments.local, self.arguments.remote, self.arguments.prune,
                                name=module[1], branches=branches)


class BuildCommand:
    """Build from the current directory using a configuration file."""

    def __init__(self, arguments):
        argparser = argparse.ArgumentParser(
            prog="git rj build",
            description="Build the contents of this repository"
        )
        argparser.add_argument(
            "-c", "--config", action="store", nargs='?', default=None,
            help="configuration to build with.")
        argparser.add_argument(
            "-r", "--release", action="store_true",
            help="build, Test, Pack, Docs in release mode.")
        argparser.add_argument(
            "-x", "--clean", action="store_true",
            help="run the clean target.")
        argparser.add_argument(
            "-b", "--build", action="store_true",
            help="build the contents of the repository.")
        argparser.add_argument(
            "-t", "--test", action="store_true",
            help="execute the test suites.")
        argparser.add_argument(
            "-p", "--pack", action="store_true",
            help="generate packages.")
        argparser.add_argument(
            "-d", "--doc", action="store_true",
            help="generate documentation.")

        self.arguments = argparser.parse_args(arguments)

        if self.arguments.release:
            if self.arguments.build or self.arguments.test or self.arguments.pack or self.arguments.doc:
                raise CommandError("Release mode is exclusive to build, test, pack and doc modes")

            # Release mode builds everything
            self.arguments.clean = True
            self.arguments.build = True
            self.arguments.test = True
            self.arguments.pack = True
            self.arguments.doc = True

        if not (self.arguments.clean or self.arguments.build or self.arguments.test or self.arguments.pack or self.arguments.doc):
            self.arguments.build = True
            self.arguments.test = True

        if (self.arguments.clean and self.arguments.test):
            # If we clean, make sure we build before we test
            self.arguments.build = True

    def execute(self):
        if (not os.path.isfile(".gitrjbuild")):
            raise CommandError("Execute from the top-most directory, or ensure the '.gitrjbuild' file exists.")

        try:
            with open(".gitrjbuild") as configFile:
                config = json.load(configFile)
                configFile.close()
        except json.decoder.JSONDecodeError as ex:
            raise CommandError(f"Error loading .gitrjbuild - {ex.msg} (Line:{ex.lineno}, Col:{ex.colno})")

        cplatform = platform.system()
        if not cplatform in config:
            raise CommandError(f"Platform '{cplatform}' not present in the configuration file '.gitrjbuild'.")

        if self.arguments.release:
            if self.arguments.config == None:
                self.arguments.config = "release"
        else:
            if self.arguments.config == None:
                self.arguments.config = "dev"

        if not self.arguments.config in config[cplatform]["build"]:
            raise CommandError(f"Configuration '{self.arguments.config}' not defined in '{cplatform}'")

        cmdconfig = config[cplatform]["build"][self.arguments.config]

        os.environ["CDIR"] = os.getcwd()
        expplatform = config[cplatform]["expansion"] if "expansion" in config[cplatform] else None
        expconfig = cmdconfig["expansion"] if "expansion" in cmdconfig else None
        expansion = Expansion(expconfig, expplatform)

        if self.arguments.clean and not "clean" in cmdconfig:
            raise CommandError(f"Cannot clean, command not defined in '{cplatform}/{self.arguments.config}'")
        if self.arguments.build and not "build" in cmdconfig:
            raise CommandError(f"Cannot build, command not defined in '{cplatform}/{self.arguments.config}'")
        if self.arguments.test and not "test" in cmdconfig:
            raise CommandError(f"Cannot test, command not defined in '{cplatform}/{self.arguments.config}'")
        if self.arguments.pack and not "pack" in cmdconfig:
            raise CommandError(f"Cannot pack, command not defined in '{cplatform}/{self.arguments.config}'")
        if self.arguments.doc and not "doc" in cmdconfig:
            raise CommandError(f"Cannot provide documentation, command not defined in '{cplatform}/{self.arguments.config}'")

        try:
            if self.arguments.clean:
                print("----------------------------------------------------------------------")
                print("-- Cleaning:", cplatform, self.arguments.config)
                print("-- Command:", expansion.expand(cmdconfig["clean"]))
                print("----------------------------------------------------------------------")
                print(flush=True)
                ProcessExe.run(expansion.expand(cmdconfig["clean"]))
                print(flush=True)
            if self.arguments.build:
                print("----------------------------------------------------------------------")
                print("-- Building:", cplatform, self.arguments.config)
                print("-- Command:", expansion.expand(cmdconfig["build"]))
                print("----------------------------------------------------------------------")
                print(flush=True)
                ProcessExe.run(expansion.expand(cmdconfig["build"]))
                print(flush=True)
            if self.arguments.test:
                print("----------------------------------------------------------------------")
                print("-- Testing", cplatform, self.arguments.config)
                print("-- Command:", expansion.expand(cmdconfig["test"]))
                print("----------------------------------------------------------------------")
                print(flush=True)
                ProcessExe.run(expansion.expand(cmdconfig["test"]))
                print(flush=True)
            if self.arguments.pack:
                print("----------------------------------------------------------------------")
                print("-- Packaging", cplatform, self.arguments.config)
                print("-- Command:", expansion.expand(cmdconfig["pack"]))
                print("----------------------------------------------------------------------")
                print(flush=True)
                ProcessExe.run(expansion.expand(cmdconfig["pack"]))
                print(flush=True)
            if self.arguments.doc:
                print("----------------------------------------------------------------------")
                print("-- Generating Documentation", cplatform, self.arguments.config)
                print("-- Command:", expansion.expand(cmdconfig["doc"]))
                print("----------------------------------------------------------------------")
                print(flush=True)
                ProcessExe.run(expansion.expand(cmdconfig["doc"]))
                print(flush=True)
        except subprocess.CalledProcessError as ex:
            print("")
            print("----------------------------------------------------------------------")
            print("-- Error")
            print("-- Command:", ex.cmd)
            print("-- Returned:", ex.returncode)
            print("----------------------------------------------------------------------")
            print("")


class PerfCommand:
    """Run performance tests using BenchmarkDotNet"""

    # Design:
    #
    # Read from the .gitrjbuild file the performance tests. The user can provide the configuration
    # entries they want to test.

    def __init__(self, arguments):
        argparser = argparse.ArgumentParser(
            prog="git rj perftest",
            description="Runs performance tests built using BenchmarkDotNet for multiple "
            "frameworks and summarize in a single location. The release versions must be "
            "already built prior.")
        argparser.add_argument(
            "project", nargs="*", default=None,
            help="List of projects to run performance tests for.")
        argparser.add_argument(
            "-r", "--results", action="store_true",
            help="Don't run benchmarks, just print results from the last run")

        self.arguments = argparser.parse_args(arguments)
        self.runperfs = { }       # A dictionary of configs pointing to array of folders

    def RunPerfs(self):
        """Get the dictionary of all configurations that were run. Each entry contains
        an array of folders with the results of the benchmarks"""
        return self.runperfs

    def execute(self):
        if (not os.path.isfile(".gitrjbuild")):
            raise CommandError("Execute from the top-most directory, or ensure the '.gitrjbuild' file exists.")

        try:
            with open(".gitrjbuild") as configFile:
                config = json.load(configFile)
                configFile.close()
        except json.decoder.JSONDecodeError as ex:
            raise CommandError(f"Error loading .gitrjbuild - {ex.msg} (Line:{ex.lineno}, Col:{ex.colno})")

        cplatform = platform.system()
        perfconfigglobal = None
        perfconfiglocal = None

        if "" in config:
            if "perf" in config[""]:
                perfconfigglobal = config[""]["perf"]

        if cplatform in config:
            if "perf" in config[cplatform]:
                perfconfiglocal = config[cplatform]["perf"]

        if len(self.arguments.project) > 0:
            for prj in self.arguments.project:
                if not prj in self.runperfs:
                    if not perfconfigglobal is None and prj in perfconfigglobal:
                        self._runperf(prj, perfconfigglobal[prj])
                    if not perfconfiglocal is None and prj in perfconfiglocal:
                        self._runperf(prj, perfconfiglocal[prj])
        else:
            if not perfconfigglobal is None and len(perfconfigglobal) > 0:
                for prj in perfconfigglobal:
                    if not prj in self.runperfs:
                        self._runperf(prj, perfconfigglobal[prj])
            if not perfconfiglocal is None and len(perfconfiglocal) > 0:
                for prj in perfconfiglocal:
                    if not prj in self.runperfs:
                        self._runperf(prj, perfconfiglocal[prj])

        # Collate the results and print.
        for prj in self.runperfs:
            perfresults = { }
            perfsummary = { }
            for run in self.runperfs[prj]:
                for root, dirs, files in os.walk(run["path"]):
                    for file in files:
                        if file.endswith(".xml"):
                            name = run["target"]
                            self._parseperf(name, str(Path(root).joinpath(file)), perfresults, perfsummary)
            if (len(perfresults) > 0):
                self._printperf(prj, perfresults, perfsummary)

    def _runperf(self, prj, config):
        for target in config:
            if not prj in self.runperfs:
                self.runperfs[prj] = [ ]
            self._runperfbinary(prj, target, config[target])

    def _runperfbinary(self, prj, target, executable):
        runfolder = str(Path("perf").joinpath(prj).joinpath(target))
        cwd = Path(os.getcwd())
        fullpath = cwd.joinpath(runfolder)
        fullexe = cwd.joinpath(executable)

        if (not self.arguments.results):
            if os.path.exists(fullpath):
                shutil.rmtree(fullpath)

            if fullexe.is_file:
                if executable.endswith(".dll"):
                    if platform.system() == "Windows":
                        cmd = f"dotnet {fullexe} -f * --join -e xml"
                    else:
                        cmd = f"dotnet {fullexe} -f '*' --join -e xml"
                if executable.endswith(".exe"):
                    if platform.system() == "Windows":
                        cmd = f"{fullexe} -f * --join -e xml"
                    else:
                        os.chmod(fullexe, 0o777)
                        cmd = f"{fullexe} -f '*' --join -e xml"

            os.makedirs(fullpath, exist_ok=True)
            ProcessExe.run(cmd, cwd=str(fullpath))

        self.runperfs[prj].append(
            { "target": target, "path": str(fullpath.joinpath("BenchmarkDotNet.Artifacts")) }
        )

    def _parseperf(self, name, perfxml, results, summary):
        root = ET.parse(perfxml).getroot()
        for c in root.findall("./Benchmarks/BenchmarkCase"):
            type = c.find("./Type").text
            method = c.find("./Method").text
            mean = c.find("./Statistics/Mean").text if c.find("./Statistics/Mean") != None else None
            median = c.find("./Statistics/Median").text if c.find("./Statistics/Median") != None else None
            stderr = c.find("./Statistics/StandardError").text if c.find("./Statistics/StandardError") != None else None

            dtype = results.get(type)
            if dtype == None:
                results[type] = { }
                dtype = results[type]
            dmethod = dtype.get(method)
            if dmethod == None:
                dtype[method] = { }
                dmethod = dtype[method]
            dname = dmethod.get(name)
            if dname == None:
                dmethod[name] = { }
                dname = dmethod[name]
            dname["mean"] = "{:.2f}".format(float(mean)) if mean != None else "-"
            dname["median"] = "{:.2f}".format(float(median)) if median != None else "-"
            dname["stderr"] = "{:.2f}".format(float(stderr)) if stderr != None else "-"

        summaryxml = root.find("./HostEnvironmentInfo")
        if summary.get(name) == None:
            summary[name] = { }
            props = summary[name]
            props["BenchmarkDotNetCaption"] = summaryxml.find("./BenchmarkDotNetCaption").text
            props["BenchmarkDotNetVersion"] = summaryxml.find("./BenchmarkDotNetVersion").text
            props["OsVersion"] = summaryxml.find("./OsVersion").text
            props["ProcessorName"] = summaryxml.find("./ProcessorName").text
            props["PhysicalProcessorCount"] = summaryxml.find("./PhysicalProcessorCount").text
            props["LogicalCoreCount"] = summaryxml.find("./LogicalCoreCount").text
            props["PhysicalCoreCount"] = summaryxml.find("./PhysicalCoreCount").text
            props["RuntimeVersion"] = summaryxml.find("./RuntimeVersion").text
            props["Architecture"] = summaryxml.find("./Architecture").text
            props["HasRyuJit"] = summaryxml.find("./HasRyuJit").text

    def _printperf(self, prj, results, summary):
        # Choose from 'mean', 'media', 'stderr'
        perffields = [ "mean", "stderr" ]

        # Get the runs captures to create the header row
        perfrun = { }
        for type in results:
            for method in results[type]:
                for name in results[type][method]:
                    if perfrun.get(name) == None:
                        perfrun[name] = True

        perftablehdr = [ f"Project '{prj}' Type", "Method" ]
        perftablehdrlen = len(perftablehdr)
        perfruncol = { }
        for field in perfrun.keys():
            fieldnamematch = re.match(r"Benchmark\.(\S+)", field)
            if (fieldnamematch == None):
                fieldname = field
            else:
                fieldname = fieldnamematch.group(1)

            first = True
            col = len(perftablehdr)
            for stat in perffields:
                if first:
                    perfruncol[field] = col
                    perftablehdr.append("{} ({})".format(stat, fieldname))
                    first = False
                else:
                    perftablehdr.append(stat)
                col += 1

        # Fill in the table
        perftable = [ ]
        for type in results:
            for method in results[type]:
                row = [ None ] * len(perftablehdr)
                row[0] = type
                row[1] = method
                for name in results[type][method]:
                    col = perfruncol[name]
                    entry = 0
                    for field in perffields:
                        row[col + entry] = results[type][method][name][field]
                        entry += 1
                perftable.append(row)

        # Print the framework configuration
        for field in perfrun.keys():
            print("```text")
            print(f"Results = {field}")
            print("")

            print("{}=v{} OS={}".format(
                summary[field]["BenchmarkDotNetCaption"],
                summary[field]["BenchmarkDotNetVersion"],
                summary[field]["OsVersion"]
            ))
            print("{}, {} CPU(s), {} logical and {} physical core(s)".format(
                summary[field]["ProcessorName"],
                summary[field]["PhysicalProcessorCount"],
                summary[field]["LogicalCoreCount"],
                summary[field]["PhysicalCoreCount"]
            ))

            if summary[field]["HasRyuJit"] == "True":
                ryu = " RyuJIT"
            else:
                ryu = ""
            print("  [HOST] : {}, {}{}".format(
                summary[field]["RuntimeVersion"],
                summary[field]["Architecture"],
                ryu
            ))
            print("```")
            print("")

        # Pretty print the table as a Markdown file
        perfwidth = [None] * len(perftable[0])
        for col in range(len(perftablehdr)):
            perfwidth[col] = len(perftablehdr[col])

        for row in perftable:
            for col in range(len(row)):
                if (row[col]) is None:
                    # There was no test result, so print it as blank.
                    row[col] = '-'
                w = len(row[col])
                if (perfwidth[col] < w):
                    perfwidth[col] = w

        # Print the Markdown table
        hdr = "|"
        for col in range(len(perfwidth)):
            hdr += " {} |".format(perftablehdr[col].ljust(perfwidth[col]))
        print(hdr)

        hdr = "|"
        for col in range(perftablehdrlen):
            hdr += ":{}-|".format("".ljust(perfwidth[col], "-"))
        for col in range(perftablehdrlen, len(perftablehdr)):
            hdr += "-{}:|".format("".ljust(perfwidth[col], "-"))
        print(hdr)

        for row in perftable:
            line = "|"
            for col in range(len(row)):
                line += " {} |".format(row[col].ljust(perfwidth[col]))
            print(line)


class Command:
    """Parse the arguments on the command line."""

    def __init__(self):
        if (len(sys.argv) < 2):
            raise ArgumentError("Must provide a basic command")
        self.command = sys.argv[1].lower()
        if (self.command == "version"):
            self.argument = VersionCommand(sys.argv[2:])
        elif (self.command == "help"):
            self.argument = HelpCommand(sys.argv[2:])
        elif (self.command == "init"):
            self.argument = InitCommand(sys.argv[2:])
        elif (self.command == "pull"):
            self.argument = PullCommand(sys.argv[2:])
        elif (self.command == "fetch"):
            self.argument = FetchCommand(sys.argv[2:])
        elif (self.command == "clean"):
            self.argument = CleanCommand(sys.argv[2:])
        elif (self.command == "status"):
            self.argument = StatusCommand(sys.argv[2:])
        elif (self.command == "checkout-branch" or self.command == "cobr"):
            self.argument = CobrCommand(sys.argv[2:])
        elif (self.command == "show-branch" or self.command == "shbr"):
            self.argument = ShbrCommand(sys.argv[2:])
        elif (self.command == "remove-branch" or self.command == "rmbr"):
            self.argument = RmbrCommand(sys.argv[2:])
        elif (self.command == "build"):
            self.argument = BuildCommand(sys.argv[2:])
        elif (self.command == "perf"):
            self.argument = PerfCommand(sys.argv[2:])
        else:
            raise ArgumentError(
                f"Unknown command {self.command} given on the command line")


def execute_command(command):
    """Execute the argument given"""

    command.argument.execute()


def check_preconditions():
    """Ensures the preconditions to this Python Script."""

    # Features required
    #  3.7+  subprocess.run() with the text parameter.

    min_major = 3
    min_minor = 6
    current_version = f"{sys.version_info.major}.{sys.version_info.minor}.{sys.version_info.micro}-{sys.version_info.releaselevel}"
    version_message = f"Invalid version of Python {current_version}. Require version {min_major}.{min_minor} or later"
    if (sys.version_info.major != min_major):
        raise EnvironmentError(version_message)
    if (sys.version_info.minor < min_minor):
        raise EnvironmentError(version_message)


def main():
    try:
        check_preconditions()
        command = Command()
    except EnvironmentError as ex:
        print(str(ex))
        sys.exit(1)
    except ArgumentError as ex:
        print(str(ex))
        sys.exit(1)

    try:
        execute_command(command)
    except CommandError as ex:
        print(f"Error: 'git rj {command.command}':")
        print("", str(ex))
        sys.exit(ex.exitcode)


if __name__ == "__main__":
    main()

#!/usr/bin/env python3

import argparse
import concurrent.futures
import os
import platform
import re
import subprocess  # Python 3.7 or later
import sys
import threading

# Global Configuration
VERSION = "1.0-alpha.20201028"
GITDEBUGLEVEL = 0
MAX_WORKERS = 8

DEFAULT_BRANCH = "master"
RELEASE_BRANCH = "release/"
DEFAULT_REMOTE = "origin"


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
        try:
            git = GitExe.run(
                ["branch", "--show-current"],
                cwd=self.top_level()
            )
            if (len(git.stdout) == 0):
                return None
            return git.stdout[0]
        except subprocess.CalledProcessError:
            # We can get here if executed in an empty repository.
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
            ["diff", "-s", "--exit-code", "--", "."],
            cwd=self.top_level(), check=False
        )
        if (git_dirty.returncode):
            return True

        git_dirty_cached = GitExe.run(
            ["diff", "-s", "--exit-code", "--cached", "--", "."],
            cwd=self.top_level(), check=False
        )
        if (git_dirty_cached.returncode):
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

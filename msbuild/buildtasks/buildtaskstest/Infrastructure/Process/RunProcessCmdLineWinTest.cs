namespace RJCP.MSBuildTasks.Infrastructure.Process
{
    using System;
    using System.ComponentModel;
    using System.Runtime.InteropServices;
    using NUnit.Framework;

    [TestFixture]
    public class RunProcessCmdLineWinTest
    {
        private static readonly object[] ArgumentTestList = {
            new object[] { null, Array.Empty<string>()},
            new object[] { "", Array.Empty<string>()},
            new object[] { "/c dir", new string[] { "/c", "dir" }},
            new object[] { "\"She said \"you can't do this!\", didn't she?\"", new string[] { "She said you", "can't", "do", "this!, didn't she?" }},
            new object[] { "test.exe \"c:\\Path With Spaces\\Ending In Backslash\\\" Arg2 Arg3", new string[] { "test.exe", "c:\\Path With Spaces\\Ending In Backslash\" Arg2 Arg3" }},
            new object[] { "test.exe \"c:\\Path With Spaces\\Ending In Backslash\\\\\" Arg2 Arg3", new string[] { "test.exe", "c:\\Path With Spaces\\Ending In Backslash\\", "Arg2", "Arg3" }},
            new object[] { "DumpArgs foo\"\"\"\"\"\"\"\"\"\"\"\"bar", new string[] { "DumpArgs", "foo\"\"\"\"bar" }},
            new object[] { "FinalProgram.exe \"first second \"\"embedded quote\"\" third\"", new string[] { "FinalProgram.exe", "first second \"embedded", "quote", "third" }},
            new object[] { "\"F\"i\"r\"s\"t S\"e\"c\"o\"n\"d\" T\"h\"i\"r\"d\"", new string[] { "First Second Third" }},
            new object[] { "F\"\"ir\"s\"\"t \\\"Second Third\"", new string[] { "Firs\"t", "\"Second", "Third" }},
            new object[] { "Firs\\\"t \\\"Second Third", new string[] { "Firs\"t", "\"Second", "Third" }},
            new object[] { "\"First \\\"Second Third", new string[] { "First \"Second Third" }},
            new object[] { "  Something Else", new string[] { "Something", "Else" }},
            new object[] { "123 456\tabc\\def\"ghi\"", new string[] { "123", "456", "abc\\defghi" }},
            new object[] { "123\"456\"\tabc", new string[] { "123456", "abc" }},
            new object[] { "\\\\SomeComputer\\subdir1\\subdir2\\", new string[] { "\\\\SomeComputer\\subdir1\\subdir2\\" }},
            new object[] { "\\\\SomeComputer\\subdir1\\subdir2\\\\", new string[] { "\\\\SomeComputer\\subdir1\\subdir2\\\\" }}
        };

        private static readonly object[] JoinedArguments = {
            new object[] { "", Array.Empty<string>()},
            new object[] { "/c dir", new string[] { "/c", "dir" }},
            new object[] { "\"She said you\" can't do \"this!, didn't she?\"", new string[] { "She said you", "can't", "do", "this!, didn't she?" }},
            new object[] { "\"c:\\Path With Spaces\\Ending In Backslash\\\" Arg2 Arg3\"", new string[] { "c:\\Path With Spaces\\Ending In Backslash\" Arg2 Arg3" }},
            new object[] { "\"c:\\Path With Spaces\\Ending In Backslash\\\\\" Arg2 Arg3", new string[] { "c:\\Path With Spaces\\Ending In Backslash\\", "Arg2", "Arg3" }},
            new object[] { "DumpArgs foo\\\"\\\"\\\"\\\"bar", new string[] { "DumpArgs", "foo\"\"\"\"bar" }},
            new object[] { "\"first second \\\"embedded\" quote third", new string[] { "first second \"embedded", "quote", "third" }},
            new object[] { "\"First Second Third\"", new string[] { "First Second Third" }},
            new object[] { "Firs\\\"t \\\"Second Third", new string[] { "Firs\"t", "\"Second", "Third" }},
            new object[] { "\"First \\\"Second Third\"", new string[] { "First \"Second Third" }},
            new object[] { "Something Else", new string[] { "Something", "Else" }},
            new object[] { "123 456 abc\\defghi", new string[] { "123", "456", "abc\\defghi" }},
            new object[] { "123456 abc", new string[] { "123456", "abc" }},
            new object[] { "\\\\SomeComputer\\subdir1\\subdir2\\", new string[] { "\\\\SomeComputer\\subdir1\\subdir2\\" }},
            new object[] { "\\\\SomeComputer\\subdir1\\subdir2\\\\", new string[] { "\\\\SomeComputer\\subdir1\\subdir2\\\\" }}
        };

        [TestCaseSource(nameof(ArgumentTestList))]
        [TestCaseSource(nameof(JoinedArguments))]
        public void SplitArgument(string arguments, string[] expected)
        {
            string[] args = RunProcess.Windows.SplitCommandLine(arguments);
            Assert.That(args, Is.EqualTo(expected).AsCollection);
        }

        [TestCaseSource(nameof(JoinedArguments))]
        public void JoinArgument(string joined, string[] arguments)
        {
            string cmdLine = RunProcess.Windows.JoinCommandLine(arguments);
            Assert.That(cmdLine, Is.EqualTo(joined));
        }

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern IntPtr CommandLineToArgvW(
            [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine,
            out int pNumArgs);

        [DllImport("kernel32.dll")]
        private static extern IntPtr LocalFree(IntPtr hMem);

        private static string[] WindowsNativeSplitCommandLine(string arguments)
        {
            IntPtr ptrToSplitArgs = IntPtr.Zero;
            try {
                ptrToSplitArgs = CommandLineToArgvW(arguments, out int numberOfArgs);
                if (ptrToSplitArgs == IntPtr.Zero)
                    throw new ArgumentException("Cannot split arguments", new Win32Exception());

                string[] splitArgs = new string[numberOfArgs];

                // ptrToSplitArgs is an array of pointers to null terminated Unicode strings.
                // Copy each of these strings into our split argument array.
                for (int i = 0; i < numberOfArgs; i++)
                    splitArgs[i] = Marshal.PtrToStringUni(Marshal.ReadIntPtr(ptrToSplitArgs, i * IntPtr.Size));

                return splitArgs;
            } finally {
                if (ptrToSplitArgs != IntPtr.Zero)
                    LocalFree(ptrToSplitArgs);
            }
        }

        [TestCaseSource(nameof(ArgumentTestList))]
        [TestCaseSource(nameof(JoinedArguments))]
        [Platform(Include = "Win32")]
        public void SplitArgumentWin32Native(string arguments, string[] expected)
        {
            // The first parameter must look like a "program"
            string[] args = WindowsNativeSplitCommandLine("test.exe " + arguments);
            for (int i = 0; i < args.Length; i++) {
                Console.WriteLine($"Arg {i}: {args[i]}");
            }

            Assert.That(args, Has.Length.EqualTo(expected.Length + 1));
            for (int i = 1; i < args.Length; i++) {
                Assert.That(expected[i - 1], Is.EqualTo(args[i]));
            }
        }
    }
}

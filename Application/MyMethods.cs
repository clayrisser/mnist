using System;
using System.IO;

namespace mkLink {
    static class MyMethods {


        /// <summary>
        /// Whether <paramref name="path"/> names a file rather than a folder.
        /// The answer is certain when the path exists and a guess from the
        /// shape of the name when it does not.
        ///
        /// This never throws. Callers run on every keystroke in a text box, so
        /// they are always looking at half finished paths, and
        /// <c>File.GetAttributes</c> throws a different exception for each way
        /// a path can be wrong.
        /// </summary>
        public static bool IsFile(this string path) {
            if (string.IsNullOrEmpty(path)) {
                return false;
            }

            try {
                return !File.GetAttributes(path).HasFlag(FileAttributes.Directory);
            } catch (IOException) {              // missing file, missing folder, path too long
            } catch (ArgumentException) {        // empty or illegal characters
            } catch (NotSupportedException) {    // a colon somewhere other than the drive
            } catch (UnauthorizedAccessException) {
            }

            // Nothing is there to inspect, so go by the name: a trailing
            // separator means a folder, otherwise an extension means a file.
            string name = path.TrimEnd();
            for (int i = name.Length - 1; i >= 0; i--) {
                if (name[i] == '\\' || name[i] == '/') {
                    return false;
                }
                if (name[i] == '.') {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Whether anything at all is at <paramref name="path"/>. Neither
        /// <c>File.Exists</c> nor <c>Directory.Exists</c> throws.
        /// </summary>
        public static bool Exists(this string path) {
            return File.Exists(path) || Directory.Exists(path);
        }


        /// <summary>
        /// The absolute form of <paramref name="path"/>, or null when it is
        /// not a path Windows can make sense of.
        /// </summary>
        public static string FullPathOrNull(this string path) {
            if (string.IsNullOrEmpty(path)) {
                return null;
            }
            try {
                return Path.GetFullPath(path);
            } catch (ArgumentException) {
            } catch (NotSupportedException) {
            } catch (IOException) {
            } catch (System.Security.SecurityException) {
            }
            return null;
        }


        public static string FindDescription(this string type) {
            switch (type) {
                case "Symbolic Link":
                    return "Points at the target by path. Works for files and folders.";
                case "Hard Link":
                    return "A second name for the same file. Files on one volume.";
                case "Directory Junction":
                    return "A second path to the same folder. Local folders only.";
                default:
                    return "No description found.";
            }
        }
    }
}

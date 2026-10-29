using System;
using System.IO;
using System.Text;

namespace AdvancedUIToolkit.Editor
{
    /// <summary>
    /// Commits a generated C# file together with the caller's serialized manifest update.
    /// Restores the original file if manifest persistence fails; prevents stale writes when
    /// another editor or process modified the source after it was validated.
    /// The caller is responsible for restoring its serialized metadata on failure.
    /// </summary>
    internal static class AdvancedUIToolkitGenerationTransaction
    {
        /// <summary>
        /// Writes generated source and commits the corresponding manifest. Unmodified source
        /// is not rewritten, avoiding unnecessary script compilation. If the commit throws,
        /// the original source is restored (or a newly created file is deleted).
        /// </summary>
        /// <param name="path">Absolute or project-relative source destination.</param>
        /// <param name="expectedSource">Source observed and validated by the caller; null if absent.</param>
        /// <param name="newSource">Complete generated C# source text.</param>
        /// <param name="commitManifest">Callback that persists the matching binding manifest.</param>
        /// <returns>True when the C# file actually changed.</returns>
        internal static bool WriteAndCommit(string path, string expectedSource,
            string newSource, Action commitManifest)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Missing output path.", nameof(path));
            if (newSource == null) throw new ArgumentNullException(nameof(newSource));
            if (commitManifest == null) throw new ArgumentNullException(nameof(commitManifest));

            // Re-check the *validated* snapshot immediately before writing. This prevents
            // overwriting edits made while the Binder dialog or validation was open.
            bool existed = File.Exists(path);
            string actualSource = existed ? File.ReadAllText(path) : null;
            if (!string.Equals(actualSource, expectedSource, StringComparison.Ordinal))
                throw new IOException("The generated C# file changed since validation. " +
                    "Rescan/review the file before generating: " + path);

            bool sourceChanged = !string.Equals(actualSource, newSource, StringComparison.Ordinal);
            if (!sourceChanged)
            {
                commitManifest();
                return false;
            }

            string stagingPath = path + ".advanceduitoolkit-" + Guid.NewGuid().ToString("N") + ".tmp";
            string backupPath = path + ".advanceduitoolkit-" + Guid.NewGuid().ToString("N") + ".bak";
            bool wroteSource = false;
            bool preserveRecoveryBackup = false;
            try
            {
                // Create the backup before replacing anything. A failed source write
                // must leave both the old file and the old serialized manifest intact.
                if (existed) File.Copy(path, backupPath);
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
                File.WriteAllText(stagingPath, newSource, new UTF8Encoding(false));
                if (existed)
                    File.Replace(stagingPath, path, null);
                else
                    File.Move(stagingPath, path);
                wroteSource = true;

                commitManifest();
                return true;
            }
            catch (Exception originalError)
            {
                if (wroteSource)
                {
                    try
                    {
                        if (existed) File.Copy(backupPath, path, true);
                        else if (File.Exists(path)) File.Delete(path);
                    }
                    catch (Exception rollbackError)
                    {
                        // Do not destroy the recovery copy if restoring failed.
                        preserveRecoveryBackup = true;
                        throw new IOException("Generation failed and the previous C# file " +
                            "could not be restored. Recovery backup: " + backupPath,
                            new AggregateException(originalError, rollbackError));
                    }
                }
                throw;
            }
            finally
            {
                // Cleanup failures should not obscure the original generation error.
                TryDelete(stagingPath);
                if (!preserveRecoveryBackup) TryDelete(backupPath);
            }
        }

        /// <summary>Best-effort removal of a completed transaction's temporary file.</summary>
        /// <param name="path">Temporary file to remove.</param>
        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}

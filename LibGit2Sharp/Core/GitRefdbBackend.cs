using System;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitRefdbBackend
    {
        static GitRefdbBackend()
        {
            GCHandleOffset = Marshal.OffsetOf(typeof(GitRefdbBackend), "GCHandle").ToInt32();
        }

        public uint Version;

        public exists_callback Exists;
        public lookup_callback Lookup;
        public foreach_glob_callback ForeachGlob;
        public write_callback Write;
        public Delegate Rename; // TODO
        public delete_callback Delete;
        public compress_callback Compress;
        public has_log_callback HasLog; // TODO
        public ensure_log_callback EnsureLog; // TODO
        public free_callback Free;
        //public reflog_read_callback ReflogRead; // TODO
        //public reflog_write_callback ReflogWrite; // TODO
        //public reflog_rename_callback ReflogRename; // TODO
        //public reflog_delete_callback ReflogDelete; // TODO
        //public lock_callback Lock; // TODO
        //public unlock_callback Unlock; // TODO

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        /* The following static fields are not part of the structure definition. */

        public static int GCHandleOffset;

        /// <summary>
        ///   Queries the backend to determine if the given referenceName
        ///   exists.
        /// </summary>
        /// <param name="exists">[out] If the call is successful, the backend will set this to 1 if the reference exists, 0 otherwise.</param>
        /// <param name="backend">[in] A pointer to the backend which is being queried.</param>
        /// <param name="referenceName">[in] The reference name to look up.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        public delegate int exists_callback(
            out IntPtr exists,
            IntPtr backend,
            IntPtr referenceName);

        /// <summary>
        ///   Queries the backend for the given reference.
        /// </summary>
        /// <param name="referencePtr">[out] If the call is successful, the backend will set this to the reference.</param>
        /// <param name="backend">[in] A pointer to the backend which is being queried.</param>
        /// <param name="namePtr">[in] The reference to look up.</param>
        /// <returns>0 if successful; GIT_EEXISTS or an error code otherwise.</returns>
        public delegate int lookup_callback(
            out IntPtr referencePtr,
            IntPtr backend,
            IntPtr namePtr);

        /// <summary>
        ///   Iterates each reference that matches the glob pattern, using the given reference iterator.
        /// </summary>
        /// <param name="iter">[out] A pointer to the reference iterator.</param>
        /// <param name="backend">[in] A pointer to the backend to query.</param>
        /// <param name="glob">[in] A glob pattern.</param>
        /// <returns>0 if successful; GIT_EUSER or an error code otherwise.</returns>
        public delegate int foreach_glob_callback(
            out IntPtr iter,
            IntPtr backend,
            IntPtr glob);

        /// <summary>
        ///   Writes the given reference.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend to write to.</param>
        /// <param name="referencePtr">[in] The reference to write.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        public delegate int write_callback(
            IntPtr backend,
            IntPtr referencePtr);

        /// <summary>
        ///   Deletes the given reference.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend to delete.</param>
        /// <param name="referencePtr">[in] The reference to delete.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        public delegate int delete_callback(
            IntPtr backend,
            IntPtr referencePtr);

        /// <summary>
        ///   Compresses the contained references, if possible.  The backend is free to implement this in any implementation-defined way; or not at all.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend to compress.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        public delegate int compress_callback(
            IntPtr backend);

        /// <summary>
        /// The owner of this backend is finished with it. The backend is asked to clean up and shut down.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend which is being freed.</param>
        public delegate void free_callback(
            IntPtr backend);

        /// <summary>
        /// A callback for the backend's implementation of foreach.
        /// </summary>
        /// <param name="referenceName">The reference name.</param>
        /// <param name="data">Pointer to payload data passed to the caller.</param>
        /// <returns>A zero result indicates the enumeration should continue. Otherwise, the enumeration should stop.</returns>
        public delegate int foreach_callback_callback(
            IntPtr referenceName,
            IntPtr data);

        /// <summary>
        ///   Query whether a particular reference has a log (may be empty).
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend.</param>
        /// <param name="referencePtr">[in] The reference to query log.</param>
        /// <returns>1 if the log exists, 0 otherwise.</returns>
        public delegate int has_log_callback(
            IntPtr backend,
            IntPtr referencePtr);

        /// <summary>
        ///   Make sure a particular reference will have a reflog which will be appended to on writes.
        /// </summary>
        /// <param name="backend">[in] A pointer to the backend.</param>
        /// <param name="referencePtr">[in] The reference to ensure log.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        public delegate int ensure_log_callback(
            IntPtr backend,
            IntPtr referencePtr);
    }
}

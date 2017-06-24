using System;
using System.Runtime.InteropServices;

namespace LibGit2Sharp.Core
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct GitReferenceIterator
    {
        static GitReferenceIterator()
        {
            GCHandleOffset = Marshal.OffsetOf(typeof(GitReferenceIterator), "GCHandle").ToInt32();
        }

        public IntPtr GitRefdb;
        public next_callback Next;
        public next_name_callback NextName;
        public free_callback Free;

        /* The libgit2 structure definition ends here. Subsequent fields are for libgit2sharp bookkeeping. */

        public IntPtr GCHandle;

        /* The following static fields are not part of the structure definition. */

        public static int GCHandleOffset;

        /// <summary>
        ///   Return the current reference and advance the iterator.
        /// </summary>
        /// <param name="reference">[out] If the call is successful, the iterator will set this to the next git_reference struct.</param>
        /// <param name="iterator">[in] The current iterator.</param>
        /// <returns>0 if successful; an error code otherwise.</returns>
        public delegate int next_callback(
            out IntPtr reference,
            IntPtr iterator);

        /// <summary>
        ///   Return the name of the current reference and advance the iterator.
        /// </summary>
        /// <param name="reference">[out] If the call is successful, the iterator will set this to the next reference name.</param>
        /// <param name="iterator">[in] The current iterator.</param>
        /// <returns>0 if successful; GIT_EEXISTS or an error code otherwise.</returns>
        public delegate int next_name_callback(
            out IntPtr reference,
            IntPtr iterator);

        /// <summary>
        /// The owner of this iterator is finished with it. The iterator is asked to clean up and shut down.
        /// </summary>
        /// <param name="iterator">[in] A pointer to the iterator which is being freed.</param>
        public delegate void free_callback(
            IntPtr iterator);
    }
}
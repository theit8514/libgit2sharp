using System;
using System.Globalization;
using System.Runtime.InteropServices;
using LibGit2Sharp.Core;
using LibGit2Sharp.Core.Handles;

namespace LibGit2Sharp
{
    /// <summary>
    ///   Base class for all custom managed backends for the libgit2 reference database.
    /// </summary>
    public abstract class ReferenceIterator
    {
        protected ReferenceIterator(RefdbBackend backend)
        {
            this.backend = backend;
        }

        /// <summary>
        ///  If returned from Next or NextName, signals that the iteration is over.
        /// </summary>
        protected static readonly int IterationOver = (int)GitErrorCode.IterOver;

        /// <summary>
        ///  Return the current reference and advance the iterator.
        /// </summary>
        /// <param name="referenceName">The reference name of the current reference.</param>
        /// <param name="isSymbolic">True if the current reference is symbolic, false if it is direct.</param>
        /// <param name="oid">Object ID of the current reference. Valued when <paramref name="isSymbolic"/> is false.</param>
        /// <param name="target">Target of the current reference. Valued when <paramref name="isSymbolic"/> is true.</param>
        /// <returns>False if there enumeration is complete.</returns>
        public abstract bool Next(out string referenceName, out bool isSymbolic, out ObjectId oid, out string target);

        /// <summary>
        ///  Return the name of the current reference and advance the iterator
        /// </summary>
        /// <param name="referenceName">The reference name of the current reference.</param>
        /// <returns>False if there enumeration is complete.</returns>
        public abstract bool NextName(out string referenceName);

        /// <summary>
        ///  Free any data associated with this iterator.
        /// </summary>
        public abstract void Free();
        
        private readonly RefdbBackend backend;
        private IntPtr nativeReferenceIteratorPointer;
        private IntPtr nativeRefdbBackendPointer;

        internal IntPtr GitReferenceIteratorPointer
        {
            get
            {
                if (IntPtr.Zero == nativeReferenceIteratorPointer)
                {
                    var nativeReferenceIterator = new GitReferenceIterator();
                    nativeReferenceIterator.GitRefdb = GitdbBackendPointer;

                    // The "free" entry point is always provided.
                    nativeReferenceIterator.Free = BackendEntryPoints.FreeCallback;
                    nativeReferenceIterator.Next = BackendEntryPoints.NextCallback;
                    nativeReferenceIterator.NextName = BackendEntryPoints.NextNameCallback;

                    nativeReferenceIterator.GCHandle = GCHandle.ToIntPtr(GCHandle.Alloc(this));
                    nativeReferenceIteratorPointer = Marshal.AllocHGlobal(Marshal.SizeOf(nativeReferenceIterator));
                    Marshal.StructureToPtr(nativeReferenceIterator, nativeReferenceIteratorPointer, false);
                }

                return nativeReferenceIteratorPointer;
            }
        }

        internal IntPtr GitdbBackendPointer
        {
            get
            {
                if (IntPtr.Zero == nativeRefdbBackendPointer)
                {
                    nativeRefdbBackendPointer = backend.GitRefdbBackendPointer;
                }

                return nativeRefdbBackendPointer;
            }
        }

        private static class BackendEntryPoints
        {
            // Because our ReferenceIterator structure exists on the managed heap only for a short time (to be marshaled
            // to native memory with StructureToPtr), we need to bind to static delegates. If at construction time
            // we were to bind to the methods directly, that's the same as newing up a fresh delegate every time.
            // Those delegates won't be rooted in the object graph and can be collected as soon as StructureToPtr finishes.
            public static readonly GitReferenceIterator.free_callback FreeCallback = Free;
            public static readonly GitReferenceIterator.next_callback NextCallback = Next;
            public static readonly GitReferenceIterator.next_name_callback NextNameCallback = NextName;

            private static bool TryMarshalReferenceIterator(out ReferenceIterator referenceIterator, IntPtr backend)
            {
                referenceIterator = null;

                var intPtr = Marshal.ReadIntPtr(backend, GitReferenceIterator.GCHandleOffset);
                var handle = GCHandle.FromIntPtr(intPtr).Target as ReferenceIterator;

                if (handle == null)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Reference, "Cannot retrieve the ReferenceIterator handle.");
                    return false;
                }

                referenceIterator = handle;
                return true;
            }

            private static int Next(
                out IntPtr referencePtr,
                IntPtr iterator)
            {
                referencePtr = IntPtr.Zero;

                ReferenceIterator referenceIterator;
                if (!TryMarshalReferenceIterator(out referenceIterator, iterator))
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    string referenceName;
                    bool isSymbolic;
                    ObjectId oid;
                    string target;
                    if (!referenceIterator.Next(out referenceName, out isSymbolic, out oid, out target))
                    {
                        return (int)GitErrorCode.IterOver;
                    }
                    referencePtr = isSymbolic
                        ? Proxy.git_reference__alloc_symbolic(referenceName, target)
                        : Proxy.git_reference__alloc(referenceName, oid);
                    return (int)GitErrorCode.Ok;
                }
                catch (Exception ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    return (int)GitErrorCode.Error;
                }
            }

            private static int NextName(
                out IntPtr nextNamePtr,
                IntPtr iterator)
            {
                nextNamePtr = IntPtr.Zero;

                ReferenceIterator referenceIterator;
                if (!TryMarshalReferenceIterator(out referenceIterator, iterator))
                {
                    return (int)GitErrorCode.Error;
                }

                try
                {
                    string nextName;
                    if (!referenceIterator.NextName(out nextName))
                    {
                        return (int)GitErrorCode.IterOver;
                    }
                    nextNamePtr = StrictUtf8Marshaler.FromManaged(nextName);
                    return (int)GitErrorCode.Ok;
                }
                catch (Exception ex)
                {
                    Proxy.giterr_set_str(GitErrorCategory.Reference, ex);
                    return (int)GitErrorCode.Error;
                }
            }

            private static void Free(IntPtr backend)
            {
                ReferenceIterator referenceIterator;
                if (!TryMarshalReferenceIterator(out referenceIterator, backend))
                {
                    // Really? Looks weird.
                    return;
                }

                referenceIterator.Free();
            }
        }

        /// <summary>
        ///   Flags used by subclasses of RefdbBackend to indicate which operations they support.
        /// </summary>
        [Flags]
        protected enum RefdbBackendOperations
        {
            /// <summary>
            ///   This RefdbBackend declares that it supports the Compress method.
            /// </summary>
            Compress = 1,

            /// <summary>
            ///   This RefdbBackend declares that it supports the ForeachGlob method.
            /// </summary>
            ForeachGlob = 2,
        }
    }
}
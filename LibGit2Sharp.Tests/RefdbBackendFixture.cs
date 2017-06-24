using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using LibGit2Sharp.Tests.TestHelpers;
using Xunit;

namespace LibGit2Sharp.Tests
{
    public class RefdbBackendFixture : BaseFixture
    {
        [Fact]
        public void CanWriteToRefdbBackend()
        {
            string path = SandboxStandardTestRepo();

            using (var repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                repository.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), true);

                Assert.Equal(backend.References["refs/heads/newref"], new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644")));
            }
        }

        [Fact]
        public void CanReadFromRefdbBackend()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.DirectoryPath);
            using (var repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/heads/testref"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                Assert.True(repository.Refs["HEAD"].TargetIdentifier.Equals("refs/heads/testref"));
                Assert.True(repository.Refs["HEAD"].ResolveToDirectReference().TargetIdentifier.Equals("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                Branch branch = repository.Head;

                Assert.True(branch.CanonicalName.Equals("refs/heads/testref"));
            }
        }

        [Fact]
        public void CanDeleteFromRefdbBackend()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.DirectoryPath);
            using (var repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/heads/testref"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                repository.Refs.Remove("refs/heads/testref");

                Assert.True(!backend.References.ContainsKey("refs/heads/testref"));
            }
        }

        [Fact]
        public void CannotOverwriteExistingInRefdbBackend()
        {
            string path = SandboxStandardTestRepo();
            using (var repository = new Repository(path))
            {
                SetupBackend(repository);

                repository.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), false);

                Assert.Throws<NameConflictException>(() => repository.Refs.Add("refs/heads/newref", new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"), false));
            }
        }

        [Fact]
        public void CanIterateRefdbBackend()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.DirectoryPath);
            using (var repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/heads/testref"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
                backend.References["refs/heads/othersymbolic"] = new MockRefdbReference("refs/heads/testref");

                Assert.NotNull(repository.Refs.Head);
                Assert.Equal("refs/heads/testref", repository.Refs.Head.TargetIdentifier);
                Assert.True(repository.Refs.Select(r => r.CanonicalName).SequenceEqual(backend.References.Keys.Where(x => x != "HEAD")));
            }
        }

        [Fact]
        public void CanIterateTypesInRefdbBackend()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.DirectoryPath);
            using (var repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/heads/testref"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
                backend.References["refs/heads/othersymbolic"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/tags/correct1"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));

                Assert.True(repository.Tags.Select(r => r.CanonicalName).SequenceEqual(new List<string> { "refs/tags/correct1" }));
            }
        }

        [Fact]
        public void CanIterateRefdbBackendWithGlob()
        {
            var scd = new SelfCleaningDirectory(this);
            var path = Repository.Init(scd.DirectoryPath);
            using (var repository = new Repository(path))
            {
                MockRefdbBackend backend = SetupBackend(repository);

                backend.References["HEAD"] = new MockRefdbReference("refs/heads/testref");
                backend.References["refs/heads/testref"] = new MockRefdbReference(new ObjectId("be3563ae3f795b2b4353bcce3a527ad0a4f7f644"));
                backend.References["refs/heads/othersymbolic"] = new MockRefdbReference("refs/heads/testref");

                Assert.True(repository.Refs.FromGlob("refs/heads/*").Select(r => r.CanonicalName).SequenceEqual(new List<string>() { "refs/heads/othersymbolic", "refs/heads/testref" }));
                Assert.True(repository.Refs.FromGlob("refs/heads/?estref").Select(r => r.CanonicalName).SequenceEqual(new List<string>() { "refs/heads/testref" }));
            }
        }

        #region MockRefdbBackend


        /// <summary>
        ///  Kind type of a <see cref="MockRefdbReference"/>
        /// </summary>
        internal enum ReferenceType
        {
            /// <summary>
            ///  A direct reference, the target is an object ID.
            /// </summary>
            Oid = 1,

            /// <summary>
            ///  A symbolic reference, the target is another reference.
            /// </summary>
            Symbolic = 2,
        }

        internal class MockRefdbReference
        {
            public MockRefdbReference(string target)
            {
                Type = ReferenceType.Symbolic;
                Symbolic = target;
            }

            public MockRefdbReference(ObjectId target)
            {
                Type = ReferenceType.Oid;
                Oid = target;
            }

            public ReferenceType Type
            {
                get;
                private set;
            }

            public ObjectId Oid
            {
                get;
                private set;
            }

            public string Symbolic
            {
                get;
                private set;
            }

            public override int GetHashCode()
            {
                int result = 17;

                result = 37 * result + (int)Type;

                if (Type == ReferenceType.Symbolic)
                {
                    result = 37 * result + Symbolic.GetHashCode();
                }
                else
                {
                    result = 37 * result + Oid.GetHashCode();
                }

                return result;
            }

            public override bool Equals(object obj)
            {
                var other = obj as MockRefdbReference;

                if (other == null || Type != other.Type)
                {
                    return false;
                }

                if (Type == ReferenceType.Symbolic)
                {
                    return Symbolic.Equals(other.Symbolic);
                }

                return Oid.Equals(other.Oid);
            }
        }

        internal class MockRefdbBackend : RefdbBackend
        {
            private readonly SortedDictionary<string, MockRefdbReference> references =
                new SortedDictionary<string, MockRefdbReference>();

            private Action disposer;

            public MockRefdbBackend(Action disposer = null)
            {
                references.Add("HEAD", new MockRefdbReference("refs/heads/master"));
                this.disposer = disposer;
            }

            public void Dispose()
            {
                if (disposer == null)
                {
                    return;
                }

                disposer();

                disposer = null;
            }

            public SortedDictionary<string, MockRefdbReference> References
            {
                get { return references; }
            }

            public bool Compressed { get; private set; }

            protected override RefdbBackendOperations SupportedOperations
            {
                get
                {
                    return RefdbBackendOperations.Compress | RefdbBackendOperations.ForeachGlob;
                }
            }

            public override bool Exists(string referenceName)
            {
                return references.ContainsKey(referenceName);
            }

            public override bool Lookup(string referenceName, out bool isSymbolic, out ObjectId oid, out string symbolic)
            {
                MockRefdbReference reference;
                if (!references.TryGetValue(referenceName, out reference) || reference == null)
                {
                    isSymbolic = false;
                    oid = null;
                    symbolic = null;
                    return false;
                }

                isSymbolic = reference.Type == ReferenceType.Symbolic;
                oid = reference.Oid;
                symbolic = reference.Symbolic;
                return true;
            }

            public override int ForeachGlob(out ReferenceIterator iterator, string glob)
            {
                var refs = references.AsEnumerable();

                refs = refs.Where(kvp => kvp.Key != "HEAD");
                if (glob != null)
                {
                    var globRegex = new Regex("^" +
                                              Regex.Escape(glob).Replace(@"\*", ".*").Replace(@"\?", ".") +
                                              "$");
                    refs = refs.Where(kvp => globRegex.IsMatch(kvp.Key));
                }

                iterator = new MockReferenceIterator(this, refs.GetEnumerator());

                return 0;
            }

            public override void WriteDirectReference(string referenceCanonicalName, ObjectId target)
            {
                var storage = new MockRefdbReference(target);
                references.Add(referenceCanonicalName, storage);
            }

            public override void WriteSymbolicReference(string referenceCanonicalName, string targetCanonicalName)
            {
                var storage = new MockRefdbReference(targetCanonicalName);
                references.Add(referenceCanonicalName, storage);
            }

            public override void Delete(string referenceCanonicalName)
            {
                references.Remove(referenceCanonicalName);
            }

            public override void Compress()
            {
                Compressed = true;
            }

            public override bool HasLog(string referenceCanonicalName)
            {
                return false;
            }

            protected override void EnsureLog(string referenceCanonicalName)
            {
            }

            public override void Free()
            {
                Dispose();
            }

            private class MockReferenceIterator : ReferenceIterator
            {
                private readonly IEnumerator<KeyValuePair<string, MockRefdbReference>> keys;

                public MockReferenceIterator(
                    RefdbBackend backend,
                    IEnumerator<KeyValuePair<string, MockRefdbReference>> keys)
                    : base(backend)
                {
                    this.keys = keys;
                }

                public override bool Next(out string referenceName, out bool isSymbolic, out ObjectId oid, out string target)
                {
                    referenceName = null;
                    isSymbolic = false;
                    oid = ObjectId.Zero;
                    target = null;
                    if (!keys.MoveNext())
                        return false;
                    KeyValuePair<string, MockRefdbReference> current = keys.Current;
                    referenceName = current.Key;
                    isSymbolic = current.Value.Type == ReferenceType.Symbolic;
                    if (isSymbolic)
                        target = current.Value.Symbolic;
                    else
                        oid = current.Value.Oid;
                    return true;
                }

                public override bool NextName(out string nextName)
                {
                    nextName = null;
                    if (!keys.MoveNext())
                        return false;
                    KeyValuePair<string, MockRefdbReference> current = keys.Current;
                    nextName = current.Key;
                    return true;
                }

                public override void Free()
                {
                    keys.Dispose();
                }
            }
        }

        #endregion

        private static MockRefdbBackend SetupBackend(Repository repository)
        {
            MockRefdbBackend backend = null;
            Action cleanup = () =>
            {
                backend.References.Clear();
            };
            backend = new MockRefdbBackend(cleanup);
            repository.Refs.SetBackend(backend);

            return backend;
        }
    }
}

using System.Collections.Generic;
using FluentAssertions;
using Newtonsoft.Json.Linq;
using NSubstitute;
using NUnit.Framework;
using Serilog;
using Trash.Cache;
using Trash.Radarr.CustomFormat;
using Trash.Radarr.CustomFormat.Models;
using Trash.Radarr.CustomFormat.Models.Cache;
using Trash.Radarr.CustomFormat.Processors.PersistenceSteps;

namespace Trash.Tests.Radarr.CustomFormat
{
    [TestFixture]
    [Parallelizable(ParallelScope.All)]
    public class CachePersisterTest
    {
        private class Context
        {
            public Context()
            {
                Log = Substitute.For<ILogger>();
                ServiceCache = Substitute.For<IServiceCache>();
                Persister = new CachePersister(Log, ServiceCache);
            }

            public CachePersister Persister { get; }
            public ILogger Log { get; }
            public IServiceCache ServiceCache { get; }
        }

        private ProcessedCustomFormatData QuickMakeCf(string cfName, string trashId, int cfId)
        {
            return new(cfName, trashId, new JObject())
            {
                CacheEntry = new TrashIdMapping(trashId, cfName) {CustomFormatId = cfId}
            };
        }

        [Test]
        public void Cf_cache_is_valid_after_successful_load()
        {
            var ctx = new Context();
            var testCfObj = new CustomFormatCache();
            ctx.ServiceCache.Load<CustomFormatCache>().Returns(testCfObj);

            ctx.Persister.Load();
            ctx.Persister.CfCache.Should().BeSameAs(testCfObj);
        }

        [Test]
        public void Cf_cache_returns_null_if_not_loaded()
        {
            var ctx = new Context();
            ctx.Persister.Load();
            ctx.Persister.CfCache.Should().BeNull();
        }

        [Test]
        public void Save_works_with_valid_cf_cache()
        {
            var ctx = new Context();
            var testCfObj = new CustomFormatCache();
            ctx.ServiceCache.Load<CustomFormatCache>().Returns(testCfObj);

            ctx.Persister.Load();
            ctx.Persister.Save();

            ctx.ServiceCache.Received().Save(Arg.Is(testCfObj));
        }

        [Test]
        public void Saving_without_loading_does_nothing()
        {
            var ctx = new Context();
            ctx.Persister.Save();
            ctx.ServiceCache.DidNotReceive().Save(Arg.Any<object>());
        }

        [Test]
        public void Updating_overwrites_previous_cf_cache_and_updates_cf_data()
        {
            var ctx = new Context();

            // Load initial CfCache just to test that it gets replaced
            var testCfObj = new CustomFormatCache
            {
                TrashIdMappings = new List<TrashIdMapping> {new("", "") {CustomFormatId = 5}}
            };
            ctx.ServiceCache.Load<CustomFormatCache>().Returns(testCfObj);
            ctx.Persister.Load();

            // Update with new cached items
            var results = new CustomFormatTransactionData();
            results.NewCustomFormats.Add(QuickMakeCf("cfname", "trashid", 10));

            var customFormatData = new List<ProcessedCustomFormatData>
            {
                new("", "trashid", new JObject()) {CacheEntry = new TrashIdMapping("trashid", "cfname", 10)}
            };

            ctx.Persister.Update(customFormatData);
            ctx.Persister.CfCache.Should().BeEquivalentTo(new CustomFormatCache
            {
                TrashIdMappings = new List<TrashIdMapping> {customFormatData[0].CacheEntry!}
            });

            customFormatData.Should().ContainSingle()
                .Which.CacheEntry.Should().BeEquivalentTo(
                    new TrashIdMapping("trashid", "cfname") {CustomFormatId = 10});
        }

        [Test]
        public void Updating_sets_cf_cache_without_loading()
        {
            var ctx = new Context();
            ctx.Persister.Update(new List<ProcessedCustomFormatData>());
            ctx.Persister.CfCache.Should().NotBeNull();
        }

        [Test]
        public void Accept_loaded_cache_when_versions_match()
        {
            var ctx = new Context();

            var testCfObj = new CustomFormatCache
            {
                Version = CustomFormatCache.LatestVersion,
                TrashIdMappings = new List<TrashIdMapping> {new("", "", 5)}
            };
            ctx.ServiceCache.Load<CustomFormatCache>().Returns(testCfObj);
            ctx.Persister.Load();
            ctx.Persister.CfCache.Should().NotBeNull();
        }

        [TestCase(CustomFormatCache.LatestVersion-1)]
        [TestCase(CustomFormatCache.LatestVersion+1)]
        public void Set_loaded_cache_to_null_if_versions_mismatch(int versionToTest)
        {
            var ctx = new Context();

            var testCfObj = new CustomFormatCache
            {
                Version = versionToTest,
                TrashIdMappings = new List<TrashIdMapping> {new("", "", 5)}
            };
            ctx.ServiceCache.Load<CustomFormatCache>().Returns(testCfObj);
            ctx.Persister.Load();
            ctx.Persister.CfCache.Should().BeNull();
        }
    }
}
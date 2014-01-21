﻿namespace SoundFingerprinting.Dao.RAM
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;

    using SoundFingerprinting.Data;
    using SoundFingerprinting.Infrastructure;

    internal class HashBinDao : IHashBinDao
    {
        private readonly IRAMStorage storage;

        public HashBinDao()
            : this(DependencyResolver.Current.Get<IRAMStorage>())
        {
            // no op
        }

        public HashBinDao(IRAMStorage storage)
        {
            this.storage = storage;
        }

        public void Insert(long[] hashBins, long subFingerprintId, int trackId)
        {
            int table = 1;
            foreach (var hashTable in storage.HashTables)
            {
                if (!hashTable.Value.ContainsKey(hashBins[table - 1]))
                {
                    hashTable.Value[hashBins[table - 1]] = new List<long>();
                }

                lock (((ICollection)hashTable.Value[hashBins[table - 1]]).SyncRoot)
                {
                    hashTable.Value[hashBins[table - 1]].Add(subFingerprintId);
                }

                table++;
            }
        }

        public IList<HashBinData> ReadHashBinsByHashTable(int hashTableId)
        {
            if (storage.HashTables.ContainsKey(hashTableId))
            {
                var hashTable = storage.HashTables[hashTableId];
                List<HashBinData> hashBins = new List<HashBinData>();
                foreach (var hashBinPair in hashTable)
                {
                    foreach (var subFingerprintId in hashBinPair.Value)
                    {
                        HashBinData hashBin = new HashBinData
                            {
                                HashTable = hashTableId,
                                HashBin = hashBinPair.Key,
                                SubFingerprintReference = new ModelReference<long>(subFingerprintId)
                            };
                        hashBins.Add(hashBin);
                    }
                }

                return hashBins;
            }

            return Enumerable.Empty<HashBinData>().ToList();
        }

        public IList<HashData> ReadHashDataByTrackId(int trackId)
        {
            var subFingerprintsIds = storage.SubFingerprints.Where(pair => ((ModelReference<int>)pair.Value.TrackReference).Id == trackId).ToList();
            List<HashData> hashes = new List<HashData>();
            foreach (var subFingerprint in subFingerprintsIds)
            {
                var hashBuckets = new List<long>();
                foreach (var hashTable in storage.HashTables)
                {
                    foreach (var hashBucket in hashTable.Value)
                    {
                        if (hashBucket.Value.Contains(subFingerprint.Key))
                        {
                            hashBuckets.Add(hashBucket.Key);
                            break;
                        }
                    }
                }

                hashes.Add(new HashData(subFingerprint.Value.Signature, hashBuckets.ToArray()));
            }

            return hashes;
        }

        public IEnumerable<SubFingerprintData> ReadSubFingerprintDataByHashBucketsWithThreshold(
            long[] hashBuckets, int thresholdVotes)
        {
            int table = 1;
            Dictionary<long, int> subFingeprintCount = new Dictionary<long, int>();
            foreach (var hashBin in hashBuckets)
            {
                if (storage.HashTables[table].ContainsKey(hashBin))
                {
                    foreach (var subFingerprintId in storage.HashTables[table][hashBin])
                    {
                        if (subFingeprintCount.ContainsKey(subFingerprintId))
                        {
                            subFingeprintCount[subFingerprintId]++;
                        }
                        else
                        {
                            subFingeprintCount[subFingerprintId] = 1;
                        }
                    }
                }

                table++;
            }

            return subFingeprintCount.Where(pair => pair.Value >= thresholdVotes)
                                     .Select(pair => storage.SubFingerprints[pair.Key]);
        }
    }
}

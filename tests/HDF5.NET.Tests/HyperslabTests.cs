﻿using HDF.PInvoke;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Xunit;
using Xunit.Abstractions;

namespace HDF5.NET.Tests.Reading
{
    public class HyperslabTests
    {
        // https://support.hdfgroup.org/HDF5/doc/H5.user/Chunking.html
        // "The write will fail because the selection goes beyond the extent of the dimension" (https://support.hdfgroup.org/HDF5/Tutor/selectsimple.html)
        // -> hyperslab's actual points are not allowed to exceed the extend of the dimension

        // Restrictions:
        // - The maximum number of elements in a chunk is 232 - 1 which is equal to 4,294,967,295.
        // - The maximum size for any chunk is 4GB.
        // - The size of a chunk cannot exceed the size of a fixed-size dataset. For example, a 
        //   dataset consisting of a 5x4 fixed-size array cannot be defined with 10x10 chunks.
        // https://support.hdfgroup.org/images/tutr-subset2.png

        private readonly ITestOutputHelper _logger;

        public HyperslabTests(ITestOutputHelper logger)
        {
            _logger = logger;
        }

        [Fact]
        /*    /                                         /
         *   /  Dataset dimensions:  [14, 14, 14]      / 14
         *  /     Chunk dimensions:  [3, 3, 3]        /
         * /_  _  _  _  _  _  _  _  _  _  _  _  _  _ /
         * |       |        |        |        |     |
         * |       |        |        |        |     |
         * |_  _  _| X  X  _| _  _  X| X  _  _| _  _|
         * |       | X  X   |       X| X      |     |
         * |       | X  X   |       X| X      |     |
         * |_  _  _| _  _  _| _  _  _| _  _  _| _  _|
         * |       |        |        |        |     | 14
         * |       |        |        |        |     |
         * |_  _  _| X  X  _| _  _  X| X  _  _| _  _|
         * |       | X  X   |       X| X      |     |
         * |       | X  X   |       X| X      |     |   /
         * |_  _  _| _  _  _| _  _  _| _  _  _| _  _|  /
         * |       |        |        |        |     | /
         * |_  _  _| _  _  _| _  _  _| _  _  _| _  _|/
         *                    14
         */
        public void CanWalk()
        {
            // Arrange
            var dims = new ulong[] { 14, 14, 14 };
            var chunkDims = new ulong[] { 3, 3, 3 };

            var selection = new HyperslabSelection(
                rank: 3,
                starts: new ulong[] { 2, 3, 2 },
                strides: new ulong[] { 6, 5, 6 },
                counts: new ulong[] { 2, 2, 2 },
                blocks: new ulong[] { 3, 2, 5 }
            );

            var expected = new Step[]
            {
                // row 0
                new Step() { Chunk = new ulong[] {0, 1, 0}, Offset = 20, Length = 1 },
                new Step() { Chunk = new ulong[] {0, 1, 1}, Offset = 18, Length = 3 },
                new Step() { Chunk = new ulong[] {0, 1, 2}, Offset = 18, Length = 1 },
                new Step() { Chunk = new ulong[] {0, 1, 2}, Offset = 20, Length = 1 },
                new Step() { Chunk = new ulong[] {0, 1, 3}, Offset = 18, Length = 3 },
                new Step() { Chunk = new ulong[] {0, 1, 4}, Offset = 18, Length = 1 },

                new Step() { Chunk = new ulong[] {0, 1, 0}, Offset = 23, Length = 1 },
                new Step() { Chunk = new ulong[] {0, 1, 1}, Offset = 21, Length = 3 },
                new Step() { Chunk = new ulong[] {0, 1, 2}, Offset = 21, Length = 1 },
                new Step() { Chunk = new ulong[] {0, 1, 2}, Offset = 23, Length = 1 },
                new Step() { Chunk = new ulong[] {0, 1, 3}, Offset = 21, Length = 3 },
                new Step() { Chunk = new ulong[] {0, 1, 4}, Offset = 21, Length = 1 },

                new Step() { Chunk = new ulong[] {0, 2, 0}, Offset = 26, Length = 1 },
                new Step() { Chunk = new ulong[] {0, 2, 1}, Offset = 24, Length = 3 },
                new Step() { Chunk = new ulong[] {0, 2, 2}, Offset = 24, Length = 1 },
                new Step() { Chunk = new ulong[] {0, 2, 2}, Offset = 26, Length = 1 },
                new Step() { Chunk = new ulong[] {0, 2, 3}, Offset = 24, Length = 3 },
                new Step() { Chunk = new ulong[] {0, 2, 4}, Offset = 24, Length = 1 },

                new Step() { Chunk = new ulong[] {0, 3, 0}, Offset = 20, Length = 1 },
                new Step() { Chunk = new ulong[] {0, 3, 1}, Offset = 18, Length = 3 },
                new Step() { Chunk = new ulong[] {0, 3, 2}, Offset = 18, Length = 1 },
                new Step() { Chunk = new ulong[] {0, 3, 2}, Offset = 20, Length = 1 },
                new Step() { Chunk = new ulong[] {0, 3, 3}, Offset = 18, Length = 3 },
                new Step() { Chunk = new ulong[] {0, 3, 4}, Offset = 18, Length = 1 },

                // row 1
                new Step() { Chunk = new ulong[] {1, 1, 0}, Offset = 2, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 1, 1}, Offset = 0, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 1, 2}, Offset = 0, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 1, 2}, Offset = 2, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 1, 3}, Offset = 0, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 1, 4}, Offset = 0, Length = 1 },

                new Step() { Chunk = new ulong[] {1, 1, 0}, Offset = 5, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 1, 1}, Offset = 3, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 1, 2}, Offset = 3, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 1, 2}, Offset = 5, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 1, 3}, Offset = 3, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 1, 4}, Offset = 3, Length = 1 },

                new Step() { Chunk = new ulong[] {1, 2, 0}, Offset = 8, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 2, 1}, Offset = 6, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 2, 2}, Offset = 6, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 2, 2}, Offset = 8, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 2, 3}, Offset = 6, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 2, 4}, Offset = 6, Length = 1 },

                new Step() { Chunk = new ulong[] {1, 3, 0}, Offset = 2, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 3, 1}, Offset = 0, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 3, 2}, Offset = 0, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 3, 2}, Offset = 2, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 3, 3}, Offset = 0, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 3, 4}, Offset = 0, Length = 1 },

                // row 2
                new Step() { Chunk = new ulong[] {1, 1, 0}, Offset = 11, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 1, 1}, Offset = 9, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 1, 2}, Offset = 9, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 1, 2}, Offset = 11, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 1, 3}, Offset = 9, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 1, 4}, Offset = 9, Length = 1 },

                new Step() { Chunk = new ulong[] {1, 1, 0}, Offset = 14, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 1, 1}, Offset = 12, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 1, 2}, Offset = 12, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 1, 2}, Offset = 14, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 1, 3}, Offset = 12, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 1, 4}, Offset = 12, Length = 1 },

                new Step() { Chunk = new ulong[] {1, 2, 0}, Offset = 17, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 2, 1}, Offset = 15, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 2, 2}, Offset = 15, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 2, 2}, Offset = 17, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 2, 3}, Offset = 15, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 2, 4}, Offset = 15, Length = 1 },

                new Step() { Chunk = new ulong[] {1, 3, 0}, Offset = 11, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 3, 1}, Offset = 9, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 3, 2}, Offset = 9, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 3, 2}, Offset = 11, Length = 1 },
                new Step() { Chunk = new ulong[] {1, 3, 3}, Offset = 9, Length = 3 },
                new Step() { Chunk = new ulong[] {1, 3, 4}, Offset = 9, Length = 1 },

                // row 3
                new Step() { Chunk = new ulong[] {2, 1, 0}, Offset = 20, Length = 1 },
                new Step() { Chunk = new ulong[] {2, 1, 1}, Offset = 18, Length = 3 },
                new Step() { Chunk = new ulong[] {2, 1, 2}, Offset = 18, Length = 1 },
                new Step() { Chunk = new ulong[] {2, 1, 2}, Offset = 20, Length = 1 },
                new Step() { Chunk = new ulong[] {2, 1, 3}, Offset = 18, Length = 3 },
                new Step() { Chunk = new ulong[] {2, 1, 4}, Offset = 18, Length = 1 },

                new Step() { Chunk = new ulong[] {2, 1, 0}, Offset = 23, Length = 1 },
                new Step() { Chunk = new ulong[] {2, 1, 1}, Offset = 21, Length = 3 },
                new Step() { Chunk = new ulong[] {2, 1, 2}, Offset = 21, Length = 1 },
                new Step() { Chunk = new ulong[] {2, 1, 2}, Offset = 23, Length = 1 },
                new Step() { Chunk = new ulong[] {2, 1, 3}, Offset = 21, Length = 3 },
                new Step() { Chunk = new ulong[] {2, 1, 4}, Offset = 21, Length = 1 },

                new Step() { Chunk = new ulong[] {2, 2, 0}, Offset = 26, Length = 1 },
                new Step() { Chunk = new ulong[] {2, 2, 1}, Offset = 24, Length = 3 },
                new Step() { Chunk = new ulong[] {2, 2, 2}, Offset = 24, Length = 1 },
                new Step() { Chunk = new ulong[] {2, 2, 2}, Offset = 26, Length = 1 },
                new Step() { Chunk = new ulong[] {2, 2, 3}, Offset = 24, Length = 3 },
                new Step() { Chunk = new ulong[] {2, 2, 4}, Offset = 24, Length = 1 },

                new Step() { Chunk = new ulong[] {2, 3, 0}, Offset = 20, Length = 1 },
                new Step() { Chunk = new ulong[] {2, 3, 1}, Offset = 18, Length = 3 },
                new Step() { Chunk = new ulong[] {2, 3, 2}, Offset = 18, Length = 1 },
                new Step() { Chunk = new ulong[] {2, 3, 2}, Offset = 20, Length = 1 },
                new Step() { Chunk = new ulong[] {2, 3, 3}, Offset = 18, Length = 3 },
                new Step() { Chunk = new ulong[] {2, 3, 4}, Offset = 18, Length = 1 },

                // row 4
                new Step() { Chunk = new ulong[] {3, 1, 0}, Offset = 2, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 1, 1}, Offset = 0, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 1, 2}, Offset = 0, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 1, 2}, Offset = 2, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 1, 3}, Offset = 0, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 1, 4}, Offset = 0, Length = 1 },

                new Step() { Chunk = new ulong[] {3, 1, 0}, Offset = 5, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 1, 1}, Offset = 3, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 1, 2}, Offset = 3, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 1, 2}, Offset = 5, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 1, 3}, Offset = 3, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 1, 4}, Offset = 3, Length = 1 },

                new Step() { Chunk = new ulong[] {3, 2, 0}, Offset = 8, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 2, 1}, Offset = 6, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 2, 2}, Offset = 6, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 2, 2}, Offset = 8, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 2, 3}, Offset = 6, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 2, 4}, Offset = 6, Length = 1 },

                new Step() { Chunk = new ulong[] {3, 3, 0}, Offset = 2, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 3, 1}, Offset = 0, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 3, 2}, Offset = 0, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 3, 2}, Offset = 2, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 3, 3}, Offset = 0, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 3, 4}, Offset = 0, Length = 1 },

                // row 5
                new Step() { Chunk = new ulong[] {3, 1, 0}, Offset = 11, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 1, 1}, Offset = 9, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 1, 2}, Offset = 9, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 1, 2}, Offset = 11, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 1, 3}, Offset = 9, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 1, 4}, Offset = 9, Length = 1 },

                new Step() { Chunk = new ulong[] {3, 1, 0}, Offset = 14, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 1, 1}, Offset = 12, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 1, 2}, Offset = 12, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 1, 2}, Offset = 14, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 1, 3}, Offset = 12, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 1, 4}, Offset = 12, Length = 1 },

                new Step() { Chunk = new ulong[] {3, 2, 0}, Offset = 17, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 2, 1}, Offset = 15, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 2, 2}, Offset = 15, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 2, 2}, Offset = 17, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 2, 3}, Offset = 15, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 2, 4}, Offset = 15, Length = 1 },

                new Step() { Chunk = new ulong[] {3, 3, 0}, Offset = 11, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 3, 1}, Offset = 9, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 3, 2}, Offset = 9, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 3, 2}, Offset = 11, Length = 1 },
                new Step() { Chunk = new ulong[] {3, 3, 3}, Offset = 9, Length = 3 },
                new Step() { Chunk = new ulong[] {3, 3, 4}, Offset = 9, Length = 1 }
            };

            // Act
            var actual = HyperslabUtils
                .Walk(rank: 3, dims, chunkDims, selection)
                .ToArray();

            // Assert
            for (int i = 0; i < actual.Length; i++)
            {
                var actual_current = actual[i];
                var expected_current = expected[i];

                Assert.Equal(actual_current.Chunk[0], expected_current.Chunk[0]);
                Assert.Equal(actual_current.Chunk[1], expected_current.Chunk[1]);
                Assert.Equal(actual_current.Chunk[2], expected_current.Chunk[2]);
                Assert.Equal(actual_current.Offset, expected_current.Offset);
                Assert.Equal(actual_current.Length, expected_current.Length);
            }
        }

        [Fact(Skip = "Only for performance tests.")]
        public void WalkPerformance()
        {
            // Arrange
            var dims = new ulong[] { 1000, 200, 200 };
            var chunkDims = new ulong[] { 1000, 200, 200 };

            var selection = new HyperslabSelection(
                rank: 3,
                starts: new ulong[] { 1, 1, 1 },
                strides: new ulong[] { 31, 31, 31 },
                counts: new ulong[] { 30, 6, 6 },
                blocks: new ulong[] { 25, 26, 27 }
            );

            var sw = Stopwatch.StartNew();

            // Act
            for (int i = 0; i < 10; i++)
            {
                HyperslabUtils
                    .Walk(rank: 3, dims, chunkDims, selection)
                    .ToArray();
            }

            // Assert
            _logger.WriteLine($"Elapsed: {sw.Elapsed.TotalMilliseconds} ms");
        }

        [Theory]
        [InlineData(new ulong[] { 1, 3 }, new ulong[] { 1, 2, 3 }, new ulong[] { 1, 2, 3 }, new ulong[] { 1, 2, 3 })]
        [InlineData(new ulong[] { 1, 2, 3 }, new ulong[] { 1, 3 }, new ulong[] { 1, 2, 3 }, new ulong[] { 1, 2, 3 })]
        [InlineData(new ulong[] { 1, 2, 3 }, new ulong[] { 1, 2, 3 }, new ulong[] { 1, 3 }, new ulong[] { 1, 2, 3 })]
        [InlineData(new ulong[] { 1, 2, 3 }, new ulong[] { 1, 2, 3 }, new ulong[] { 1, 2, 3 }, new ulong[] { 1, 3 })]
        public void HyperslabSelectionThrowsForInvalidRank(ulong[] start, ulong[] stride, ulong[] count, ulong[] block)
        {
            // Arrange

            // Act
            Action action = () => new HyperslabSelection(rank: 3, start, stride, count, block);

            // Assert
            Assert.Throws<RankException>(action);
        }

        [Theory]
        [InlineData(new ulong[] { 1, 2, 3 }, new ulong[] { 1, 2, 0 }, new ulong[] { 1, 2, 3 }, new ulong[] { 1, 2, 3 })]
        [InlineData(new ulong[] { 1, 2, 3 }, new ulong[] { 1, 2, 2 }, new ulong[] { 1, 2, 3 }, new ulong[] { 1, 2, 3 })]
        public void HyperslabSelectionThrowsForInvalidStride(ulong[] start, ulong[] stride, ulong[] count, ulong[] block)
        {
            // Arrange

            // Act
            Action action = () => new HyperslabSelection(rank: 3, start, stride, count, block);

            // Assert
            Assert.Throws<ArgumentException>(action);
        }

        [Theory]
        [InlineData(new ulong[] { 16, 30 }, new ulong[] { 16, 25, 30 } )]
        [InlineData(new ulong[] { 16, 25, 30 }, new ulong[] { 3, 3 })]
        public void WalkThrowsForInvalidRank(ulong[] dims, ulong[] chunkDims)
        {
            // Arrange
            var selection = new HyperslabSelection(1, 4, 3, 3);

            // Act
            Action action = () => HyperslabUtils.Walk(rank: 3, dims, chunkDims, selection).ToArray();

            // Assert
            Assert.Throws<RankException>(action);
        }

        [Theory]
        [InlineData(
            2, new ulong[] { 1, 1 }, new ulong[] { 4, 4 }, new ulong[] { 4, 4 }, new ulong[] { 2, 3 },
            1, new ulong[] { 1 }, new ulong[] { 3 }, new ulong[] { 5 }, new ulong[] { 2 })]
        [InlineData(
            1, new ulong[] { 1 }, new ulong[] { 4 }, new ulong[] { 4 }, new ulong[] { 3 },
            2, new ulong[] { 1, 1 }, new ulong[] { 4, 4 }, new ulong[] { 5, 5 }, new ulong[] { 3, 2 })]
        public void CopyThrowsForInvalidRank(
            int sourceRank, ulong[] sourceStarts, ulong[] sourceStrides, ulong[] sourceCounts, ulong[] sourceBlocks,
            int targetRank, ulong[] targetStarts, ulong[] targetStrides, ulong[] targetCounts, ulong[] targetBlocks)
        {
            // Arrange
            var sourceSelection = new HyperslabSelection(rank: sourceRank, sourceStarts, sourceStrides, sourceCounts, sourceBlocks);
            var targetSelection = new HyperslabSelection(rank: targetRank, targetStarts, targetStrides, targetCounts, targetBlocks);

            var copyInfo = new CopyInfo(
                null,
                null,
                null,
                null,
                sourceSelection,
                targetSelection,
                null,
                null,
                null,
                0
            );

            // Act
            Action action = () => HyperslabUtils.Copy(sourceRank: 2, targetRank: 2, copyInfo);

            // Assert
            Assert.Throws<RankException>(action);
        }

        [Theory]
        [InlineData(
            new ulong[] { 1, 1 }, new ulong[] { 4, 4 }, new ulong[] { 4, 4 }, new ulong[] { 2, 3 },
            new ulong[] { 1, 1 }, new ulong[] { 3, 3 }, new ulong[] { 5, 5 }, new ulong[] { 2, 3 })]
        [InlineData(
            new ulong[] { 1, 1 }, new ulong[] { 4, 4 }, new ulong[] { 4, 4 }, new ulong[] { 3, 4 },
            new ulong[] { 1, 1 }, new ulong[] { 4, 4 }, new ulong[] { 5, 5 }, new ulong[] { 3, 2 })]
        public void CopyThrowsForMismatchingSelectionSizes(
            ulong[] sourceStarts, ulong[] sourceStrides, ulong[] sourceCounts, ulong[] sourceBlocks,
            ulong[] targetStarts, ulong[] targetStrides, ulong[] targetCounts, ulong[] targetBlocks)
        {
            // Arrange
            var sourceSelection = new HyperslabSelection(rank: 2, sourceStarts, sourceStrides, sourceCounts, sourceBlocks);
            var targetSelection = new HyperslabSelection(rank: 2, targetStarts, targetStrides, targetCounts, targetBlocks);

            var copyInfo = new CopyInfo(
                null,
                null,
                null,
                null,
                sourceSelection,
                targetSelection,
                null,
                null,
                null,
                0
            );

            // Act
            Action action = () => HyperslabUtils.Copy(sourceRank: 2, targetRank: 2, copyInfo);

            // Assert
            Assert.Throws<ArgumentException>(action);
        }

        [Theory]
        [InlineData(new ulong[] { 0, 2 }, new ulong[] { 3, 2 })]
        [InlineData(new ulong[] { 2, 2 }, new ulong[] { 0, 2 })]
        public void CanCopySmall2D_Count0_Or_Block0(ulong[] counts, ulong[] blocks)
        {
            // Arrange
            var datasetDims = new ulong[] { 10, 10 };
            var chunkDims = new ulong[] { 6, 6 };
            var memoryDims = new ulong[] { 10, 10 };

            var datasetSelection = new HyperslabSelection(
                rank: 2,
                starts: new ulong[] { 0, 1 },
                strides: new ulong[] { 5, 5 },
                counts: counts,
                blocks: blocks
            );

            var memorySelection = new HyperslabSelection(
                rank: 2,
                starts: new ulong[] { 1, 1 },
                strides: new ulong[] { 5, 5 },
                counts: counts,
                blocks: blocks
            );

            var copyInfo = new CopyInfo(
                datasetDims,
                chunkDims,
                memoryDims,
                memoryDims,
                datasetSelection,
                memorySelection,
                indices => null,
                indices => null,
                indices => null,
                TypeSize: 4
            );

            // Act
            HyperslabUtils.Copy(sourceRank: 2, targetRank: 2, copyInfo);

            // Assert
        }

        [Theory]
        [InlineData(new ulong[] { 10, 0 }, new ulong[] { 10, 10 })]
        [InlineData(new ulong[] { 10, 10 }, new ulong[] { 10, 0 })]
        public void CanCopySmall2D_Dims0(ulong[] datasetDims, ulong[] memoryDims)
        {
            // Arrange
            var chunkDims = new ulong[] { 6, 6 };

            var datasetSelection = new HyperslabSelection(
                rank: 2,
                starts: new ulong[] { 0, 1 },
                strides: new ulong[] { 5, 5 },
                counts: new ulong[] { 2, 0 },
                blocks: new ulong[] { 3, 2 }
            );

            var memorySelection = new HyperslabSelection(
                rank: 2,
                starts: new ulong[] { 1, 1 },
                strides: new ulong[] { 5, 5 },
                counts: new ulong[] { 2, 0 },
                blocks: new ulong[] { 3, 2 }
            );

            var copyInfo = new CopyInfo(
                datasetDims,
                chunkDims,
                memoryDims,
                memoryDims,
                datasetSelection,
                memorySelection,
                indices => null,
                indices => null,
                indices => null,
                TypeSize: 4
            );

            // Act
            HyperslabUtils.Copy(sourceRank: 2, targetRank: 2, copyInfo);

            // Assert        
        }

        [Fact]
        /* Source:
         *    /                             /
         *   /  Dataset dimensions:  [10, 10]
         *  /     Chunk dimensions:  [6, 6]
         * /_  _  _  _  _  _  _  _  _  _ /
         * |   X  X         | X  X      |
         * |   X  X         | X  X      |
         * |   X  X         | X  X      |
         * |                |           |
         * |                |           | 10
         * |_  X  X  _  _  _| X  X  _  _| 
         * |   X  X         | X  X      |
         * |   X  X         | X  X      |
         * |                |           | /
         * |_  _  _  _  _  _| _  _  _  _|/
         *               10
         */

        /* Target:
         *    /                             /
         *   /  Dataset dimensions:  [10, 10]
         *  /     Chunk dimensions:  [10, 10]
         * /_  _  _  _  _  _  _  _  _  _ /
         * |                            |
         * |   X  X  X  X  X  X         |
         * |   X  X  X  X  X  X         |
         * |                            |
         * |                            | 10
         * |   X  X  X  X  X  X         | 
         * |   X  X  X  X  X  X         |
         * |                            |
         * |                            | /
         * |_  _  _  _  _  _  _  _  _  _|/
         *               10
         */
        public void CanCopySmall2D_BlockGreater1_StrideGreater1()
        {
            // Arrange
            var datasetDims = new ulong[] { 10, 10 };
            var chunkDims = new ulong[] { 6, 6 };
            var memoryDims = new ulong[] { 10, 10 };

            var datasetSelection = new HyperslabSelection(
                rank: 2,
                starts: new ulong[] { 0, 1 },
                strides: new ulong[] { 5, 5 },
                counts: new ulong[] { 2, 2 },
                blocks: new ulong[] { 3, 2 }
            );

            var memorySelection = new HyperslabSelection(
                rank: 2,
                starts: new ulong[] { 1, 1 },
                strides: new ulong[] { 4, 25 /* should work */ },
                counts: new ulong[] { 2, 1 },
                blocks: new ulong[] { 2, 6 }
            );

            // s0
            var sourceBuffer0 = new byte[6 * 6 * sizeof(int)];
            var s0 = MemoryMarshal.Cast<byte, int>(sourceBuffer0);
            s0[1] = 1; s0[2] = 2; s0[7] = 5; s0[8] = 6; s0[13] = 9; s0[14] = 10; s0[31] = 13; s0[32] = 14;

            var sourceBuffer1 = new byte[6 * 6 * sizeof(int)];
            var s1 = MemoryMarshal.Cast<byte, int>(sourceBuffer1);
            s1[0] = 3; s1[1] = 4; s1[6] = 7; s1[7] = 8; s1[12] = 11; s1[13] = 12; s1[30] = 15; s1[31] = 16;

            var sourceBuffer2 = new byte[6 * 6 * sizeof(int)];
            var s2 = MemoryMarshal.Cast<byte, int>(sourceBuffer2);
            s2[1] = 17; s2[2] = 18; s2[7] = 21; s2[8] = 22;

            var sourceBuffer3 = new byte[6 * 6 * sizeof(int)];
            var s3 = MemoryMarshal.Cast<byte, int>(sourceBuffer3);
            s3[0] = 19; s3[1] = 20; s3[6] = 23; s3[7] = 24;

            var chunksBuffers = new Memory<byte>[]
            {
                sourceBuffer0,
                sourceBuffer1, 
                sourceBuffer2,
                sourceBuffer3
            };

            var expectedBuffer = new byte[10 * 10 * sizeof(int)];
            var expected = MemoryMarshal.Cast<byte, int>(expectedBuffer);

            expected[11] = 1; expected[12] = 2; expected[13] = 3; expected[14] = 4; expected[15] = 5; expected[16] = 6;
            expected[21] = 7; expected[22] = 8; expected[23] = 9; expected[24] = 10; expected[25] = 11; expected[26] = 12;
            expected[51] = 13; expected[52] = 14; expected[53] = 15; expected[54] = 16; expected[55] = 17; expected[56] = 18;
            expected[61] = 19; expected[62] = 20; expected[63] = 21; expected[64] = 22; expected[65] = 23; expected[66] = 24;

            var actualBuffer = new byte[10 * 10 * sizeof(int)];
            var actual = MemoryMarshal.Cast<byte, int>(actualBuffer);
            var scaledDatasetDims = datasetDims.Select((dim, i) => H5Utils.CeilDiv(dim, chunkDims[i])).ToArray();

            var copyInfo = new CopyInfo(
                datasetDims,
                chunkDims,
                memoryDims,
                memoryDims,
                datasetSelection,
                memorySelection,
                indices => chunksBuffers[indices.ToLinearIndex(scaledDatasetDims)],
                indices => null,
                indices => actualBuffer,
                TypeSize: 4
            );

            // Act
            HyperslabUtils.Copy(sourceRank: 2, targetRank: 2, copyInfo);

            // Assert
            Assert.True(actual.SequenceEqual(expected));
        }

        [Fact]
        /*   DS [2 x 3 x 6]                        CHUNK [2 x 3 x 6]
         *                  |-05--11--17-|                        |<----------->|
         *               |-04--10--16-|                        |<----------->|
         *            |-03--09--15-|                        |<----------->|
         *         |-02--08--14-|                        |<----------->|
         *      |-01--07--13-|                        |<----------->|
         *   |-00--06--12-|                        |<-----01---->|
         *   |-18--24--30-|                        |<----------->|
         */

        /*                                         CHUNK [1 x 2 x 3]
         *                                                        |<------><----|
         *                                                     |<------><----|
         *                                                  |<--02--><-04-|
         *                                               |<------><----|
         *                                            |<------><----|
         *                                         |<--01--><-03-|
         *                                         |<--05--><-07-|
         */
        public void CanCopySmall3D_Block1_Stride1()
        {
            // Arrange
            var datasetDims = new ulong[] { 2, 3, 6 };
            var chunkDims = new ulong[] { 1, 2, 3 };
            var memoryDims = new ulong[] { 5, 6, 11 };

            var sourceSelection = new HyperslabSelection(rank: 3,
                starts: new ulong[] { 0, 1, 1 },
                strides: new ulong[] { 1, 1, 1 },
                counts: new ulong[] { 1, 2, 3 },
                blocks: new ulong[] { 1, 1, 1 });

            var targetSelection = new HyperslabSelection(rank: 3,
                starts: new ulong[] { 1, 1, 1 },
                strides: new ulong[] { 1, 1, 1 },
                counts: new ulong[] { 3, 1, 2 },
                blocks: new ulong[] { 1, 1, 1 });

            // source
            var sourceBuffer1 = new byte[1 * 2 * 3 * sizeof(int)];
            var s1 = MemoryMarshal.Cast<byte, int>(sourceBuffer1);
            s1[0] = 0; s1[1] = 1; s1[2] = 2; s1[3] = 6; s1[4] = 7; s1[5] = 8;

            var sourceBuffer2 = new byte[1 * 2 * 3 * sizeof(int)];
            var s2 = MemoryMarshal.Cast<byte, int>(sourceBuffer2);
            s2[0] = 3; s2[1] = 4; s2[2] = 5; s2[3] = 9; s2[4] = 10; s2[5] = 11;

            var sourceBuffer3 = new byte[1 * 2 * 3 * sizeof(int)];
            var s3 = MemoryMarshal.Cast<byte, int>(sourceBuffer3);
            s3[0] = 12; s3[1] = 13; s3[2] = 14; s3[3] = 0; s3[4] = 0; s3[5] = 0;

            var sourceBuffer4 = new byte[1 * 2 * 3 * sizeof(int)];
            var s4 = MemoryMarshal.Cast<byte, int>(sourceBuffer4);
            s4[0] = 15; s4[1] = 16; s4[2] = 17; s4[3] = 0; s4[4] = 0; s4[5] = 0;

            var sourceBuffer5 = new byte[1 * 2 * 3 * sizeof(int)];
            var s5 = MemoryMarshal.Cast<byte, int>(sourceBuffer5);
            s5[0] = 18; s5[1] = 19; s5[2] = 20; s5[3] = 24; s5[4] = 25; s5[5] = 26;

            var sourceBuffer6 = new byte[1 * 2 * 3 * sizeof(int)];
            var s6 = MemoryMarshal.Cast<byte, int>(sourceBuffer6);
            s6[0] = 21; s6[1] = 22; s6[2] = 23; s6[3] = 27; s6[4] = 28; s6[5] = 29;

            var sourceBuffer7 = new byte[1 * 2 * 3 * sizeof(int)];
            var s7 = MemoryMarshal.Cast<byte, int>(sourceBuffer7);
            s7[0] = 30; s7[1] = 31; s7[2] = 32; s7[3] = 0; s7[4] = 0; s7[5] = 0;

            var sourceBuffer8 = new byte[1 * 2 * 3 * sizeof(int)];
            var s8 = MemoryMarshal.Cast<byte, int>(sourceBuffer8);
            s8[0] = 33; s8[1] = 34; s8[2] = 35; s8[3] = 0; s8[4] = 0; s8[5] = 0;

            var chunksBuffers = new Memory<byte>[]
            {
                sourceBuffer1,
                sourceBuffer2,
                sourceBuffer3,
                sourceBuffer4,
                sourceBuffer5,
                sourceBuffer6,
                sourceBuffer7,
                sourceBuffer8,
            };

            var expectedBuffer = new byte[5 * 6 * 11 * sizeof(int)];
            var expected = MemoryMarshal.Cast<byte, int>(expectedBuffer);

            expected[78] = 7;
            expected[79] = 8;
            expected[144] = 9;
            expected[145] = 13;
            expected[210] = 14;
            expected[211] = 15;

            var actualBuffer = new byte[5 * 6 * 11 * sizeof(int)];
            var actual = MemoryMarshal.Cast<byte, int>(actualBuffer);
            var scaledDatasetDims = datasetDims.Select((dim, i) => H5Utils.CeilDiv(dim, chunkDims[i])).ToArray();

            var copyInfo = new CopyInfo(
                datasetDims,
                chunkDims,
                memoryDims,
                memoryDims,
                sourceSelection,
                targetSelection,
                indices => chunksBuffers[indices.ToLinearIndex(scaledDatasetDims)],
                indices => null,
                indices => actualBuffer,
                TypeSize: 4
            );

            // Act
            HyperslabUtils.Copy(sourceRank: 3, targetRank: 3, copyInfo);

            // Assert
            Assert.True(actual.SequenceEqual(expected));
        }

        [Fact]
        /*    /                             /
         *   /  Dataset dimensions:  [10, 10, 10]
         *  /     Chunk dimensions:  [6, 6, 3]
         * /_  _  _  _  _  _  _  _  _  _ /
         * |                |           |
         * |                |           |
         * |                |           |
         * |                |           |
         * |            X   | X     X   | 10
         * |_  _  _  _  _  _| _  _  _  _| 
         * |            X   | X     X   |
         * |                |           |
         * |                |           | /
         * |_  _  _  _  _  _| _  _  _  _|/
         *               10
         */
        public void CanCopySmall3D_Block1_StrideGreater1()
        {
            // Arrange
            var datasetDims = new ulong[] { 10, 10, 10 };
            var chunkDims = new ulong[] { 6, 6, 3 };
            var memoryDims = new ulong[] { 11, 11, 12 };

            var datasetSelection = new HyperslabSelection(
                rank: 3,
                starts: new ulong[] { 4, 4, 2 },
                strides: new ulong[] { 2, 2, 4 },
                counts: new ulong[] { 2, 3, 2 },
                blocks: new ulong[] { 1, 1, 1 }
            );

            var memorySelection = new HyperslabSelection(
                rank: 3,
                starts: new ulong[] { 2, 1, 1 },
                strides: new ulong[] { 3, 3, 3 },
                counts: new ulong[] { 2, 2, 3 },
                blocks: new ulong[] { 1, 1, 1 }
            );

            // s0
            var sourceBuffer0 = new byte[6 * 6 * 3 * sizeof(int)];
            var s0 = MemoryMarshal.Cast<byte, int>(sourceBuffer0);
            s0[86] = 1;

            // s2
            var sourceBuffer2 = new byte[6 * 6 * 3 * sizeof(int)];
            var s2 = MemoryMarshal.Cast<byte, int>(sourceBuffer2);
            s2[84] = 2;

            // s4
            var sourceBuffer4 = new byte[6 * 6 * 3 * sizeof(int)];
            var s4 = MemoryMarshal.Cast<byte, int>(sourceBuffer4);
            s4[74] = 3;
            s4[80] = 5;

            // s6
            var sourceBuffer6 = new byte[6 * 6 * 3 * sizeof(int)];
            var s6 = MemoryMarshal.Cast<byte, int>(sourceBuffer6);
            s6[72] = 4;
            s6[78] = 6;

            // s8
            var sourceBuffer8 = new byte[6 * 6 * 3 * sizeof(int)];
            var s8 = MemoryMarshal.Cast<byte, int>(sourceBuffer8);
            s8[14] = 7;

            // s10
            var sourceBuffer10 = new byte[6 * 6 * 3 * sizeof(int)];
            var s10 = MemoryMarshal.Cast<byte, int>(sourceBuffer10);
            s10[12] = 8;

            // s12
            var sourceBuffer12 = new byte[6 * 6 * 3 * sizeof(int)];
            var s12 = MemoryMarshal.Cast<byte, int>(sourceBuffer12);
            s12[2] = 9;
            s12[8] = 11;

            // s14
            var sourceBuffer14 = new byte[6 * 6 * 3 * sizeof(int)];
            var s14 = MemoryMarshal.Cast<byte, int>(sourceBuffer14);
            s14[0] = 10;
            s14[6] = 12;

            var chunksBuffers = new Memory<byte>[]
            {
                sourceBuffer0, null, sourceBuffer2, null, sourceBuffer4, null, sourceBuffer6, null,
                sourceBuffer8, null, sourceBuffer10, null, sourceBuffer12, null, sourceBuffer14, null
            };

            var expectedBuffer = new byte[11 * 11 * 12 * sizeof(int)];
            var expected = MemoryMarshal.Cast<byte, int>(expectedBuffer);
            var scaledDatasetDims = datasetDims.Select((dim, i) => H5Utils.CeilDiv(dim, chunkDims[i])).ToArray();

            expected[277] = 1;
            expected[280] = 2;
            expected[283] = 3;
            expected[313] = 4;
            expected[316] = 5;
            expected[319] = 6;
            expected[673] = 7;
            expected[676] = 8;
            expected[679] = 9;
            expected[709] = 10;
            expected[712] = 11;
            expected[715] = 12;

            var actualBuffer = new byte[11 * 11 * 12 * sizeof(int)];
            var actual = MemoryMarshal.Cast<byte, int>(actualBuffer);

            var copyInfo = new CopyInfo(
                datasetDims,
                chunkDims,
                memoryDims,
                memoryDims,
                datasetSelection,
                memorySelection,
                indices => chunksBuffers[indices.ToLinearIndex(scaledDatasetDims)],
                indices => null,
                indices => actualBuffer,
                TypeSize: 4
            );

            // Act
            HyperslabUtils.Copy(sourceRank: 3, targetRank: 3, copyInfo);

            // Assert
            Assert.True(actual.SequenceEqual(expected));
        }

        [Fact(Skip = "C library does not give correct 'expected' array :-/")]
        public void CanCopyLikeCLib()
        {
            TestUtils.RunForAllVersions(version =>
            {
                // Arrange
                var datasetDims = new ulong[] { 25, 25, 4 };
                var chunkDims = new ulong[] { 7, 20, 3 };
                var memoryDims = new ulong[] { 75, 25 };

                var datasetSelection = new HyperslabSelection(
                    rank: 3,
                    starts: new ulong[] { 2, 2, 0 },
                    strides: new ulong[] { 5, 8, 2 },
                    counts: new ulong[] { 5, 3, 2 },
                    blocks: new ulong[] { 3, 5, 2 }
                );

                var memorySelection = new HyperslabSelection(
                    rank: 2,
                    starts: new ulong[] { 2, 1 },
                    strides: new ulong[] { 35, 17 },
                    counts: new ulong[] { 2, 1 },
                    blocks: new ulong[] { 30, 15 }
                );

                var filePath = TestUtils.PrepareTestFile(version, fileId => TestUtils.AddChunkedDatasetForHyperslab(fileId));
                var expectedBuffer = new byte[memoryDims[0] * memoryDims[1] * 4];
                var expected = MemoryMarshal.Cast<byte, int>(expectedBuffer.AsSpan());

                {
                    var fileId = H5F.open(filePath, H5F.ACC_RDONLY);
                    var datasetId = H5D.open(fileId, "/chunked/hyperslab");
                    
                    var datasetSpaceId = H5S.create_simple(rank: 3, datasetDims, datasetDims);
                    var res1 = H5S.select_hyperslab(datasetSpaceId, H5S.seloper_t.SET, 
                        datasetSelection.Starts.ToArray(), datasetSelection.Strides.ToArray(),
                        datasetSelection.Counts.ToArray(), datasetSelection.Blocks.ToArray());

                    var memorySpaceId = H5S.create_simple(rank: 2, memoryDims, memoryDims);
                    var res2 = H5S.select_hyperslab(memorySpaceId, H5S.seloper_t.SET,
                        memorySelection.Starts.ToArray(), memorySelection.Strides.ToArray(),
                        memorySelection.Counts.ToArray(), memorySelection.Blocks.ToArray());

                    unsafe
                    {
                        fixed (byte* ptr = expectedBuffer)
                        {
                            var res3 = H5D.read(datasetId, H5T.NATIVE_INT32, memorySpaceId, datasetSpaceId, 0, new IntPtr(ptr));

                            if (res3 < 0)
                                throw new Exception("Unable to read data.");
                        }
                    }

                    H5S.close(memorySpaceId);
                    H5S.close(datasetSpaceId);
                    H5D.close(datasetId);
                    H5F.close(fileId);
                }

                using var root = H5File.OpenReadCore(filePath, deleteOnClose: true);
                var dataset = root.Dataset("/chunked/hyperslab");

                /* get intermediate data (only for Matlab visualization) */
                var intermediateBuffer = new byte[datasetDims[0] * datasetDims[1] * datasetDims[2] * 4];
                var intermediate = MemoryMarshal.Cast<byte, int>(intermediateBuffer);

                var chunkProviderIntermediate = H5D_Chunk.Create(dataset, default);
                chunkProviderIntermediate.Initialize();

                var copyInfoInterMediate = new CopyInfo(
                    datasetDims,
                    chunkDims,
                    datasetDims,
                    datasetDims,
                    datasetSelection,
                    datasetSelection,
                    indices => chunkProviderIntermediate.GetBuffer(indices),
                    indices => null,
                    indices => intermediateBuffer,
                    TypeSize: 4
                );

                HyperslabUtils.Copy(sourceRank: 3, targetRank: 3, copyInfoInterMediate);

                /* get actual data */
                var actualBuffer = new byte[memoryDims[0] * memoryDims[1] * 4];
                var actual = MemoryMarshal.Cast<byte, int>(actualBuffer);

                var chunkProvider = H5D_Chunk.Create(dataset, default);
                chunkProvider.Initialize();

                var copyInfo = new CopyInfo(
                    datasetDims,
                    chunkDims,
                    memoryDims,
                    memoryDims,
                    datasetSelection,
                    memorySelection,
                    indices => chunkProvider.GetBuffer(indices),
                    indices => null,
                    indices => actualBuffer,
                    TypeSize: 4
                );

                // Act
                HyperslabUtils.Copy(sourceRank: 3, targetRank: 2, copyInfo);

                //var intermediateForMatlab = string.Join(',', intermediate.ToArray().Select(value => value.ToString()));
                //var actualForMatlab = string.Join(',', actual.ToArray().Select(value => value.ToString()));
                //var expectedForMatlab = string.Join(',', expected.ToArray().Select(value => value.ToString()));

                // Assert
                Assert.True(actual.SequenceEqual(expected));
            });
        }
    }
}
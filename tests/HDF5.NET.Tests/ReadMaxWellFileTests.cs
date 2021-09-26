using System;
using System.Threading.Tasks;
using Xunit;

namespace HDF5.NET.Tests
{
    public class ReadH5MaxwellFileTests
    {
        private static H5File Root { get; set; }
        public record ElectrodeProperties(
            int Electrode,
            int Channel,
            double XuM,
            double YuM)
        {
            public int Electrode { get; } = Electrode;
            public int Channel { get; } = Channel;
            public double XuM { get; } = XuM;
            public double YuM { get; } = YuM;
        }

        [Fact]
        public void OpenAndReadH5MaxwellFileTest()
        {
            OpenTestFile();
            var unused = ReadAll_OneElectrodeAsInt(GetChannelProperties(863));
        }

        [Fact]
        public void OpenAndReadH5MaxwellFileWithThreadsTest()
        {
            OpenTestFile();
            var unused = ReadAll_OneElectrodeAsIntParallel(GetChannelProperties(863));
        }

        private ElectrodeProperties GetChannelProperties(int channel)
        {
            return new ElectrodeProperties(0, channel, 0, 0);
        }

        private void OpenTestFile()
        {
            var localFilePath = GetFileNameHdd(true);
            if (!OpenReadMaxWellFile(localFilePath))
                throw new Exception();
        }

        private string GetFileNameHdd(bool optionHdd)
        {
            return optionHdd ?  "E:\\2021 MaxWell\\Trace_20210715_16_54_48_1mM(+++).raw.h5"
                                :"C:\\Users\\fred\\Downloads\\Trace_20210715_16_54_48_1mM(+++).raw.h5";
        }


        public bool OpenReadMaxWellFile(string fileName)
        {
            Root = H5File.OpenRead(fileName);
            return Root != null;
        }

        public ushort[] ReadAll_OneElectrodeAsInt(ElectrodeProperties electrodeProperties)
        {
            var h5Group = Root.Group("/");
            var h5Dataset = h5Group.Dataset("sig");
            int ndimensions = h5Dataset.Space.Rank;
            if (ndimensions != 2)
                return null;
            var nbdatapoints = h5Dataset.Space.Dimensions[1];
            return Read_OneElectrodeDataAsInt(h5Dataset, electrodeProperties.Channel, 0, nbdatapoints - 1);
        }

        public ushort[] ReadAll_OneElectrodeAsIntParallel(ElectrodeProperties electrodeProperties)
        {
            var h5Group = Root.Group("/");
            var h5Dataset = h5Group.Dataset("sig");
            var nbdatapoints = h5Dataset.Space.Dimensions[1];
            const ulong chunkSizePerChannel = 200;
            var result = new ushort[nbdatapoints];
            var nchunks = (long)(nbdatapoints / chunkSizePerChannel);

            int ndimensions = h5Dataset.Space.Rank;
            if (ndimensions != 2)
                return null;

            var fileName = GetFileNameHdd(true);
            Parallel.For(0, nchunks, i =>
            {
                var lRoot = H5File.OpenRead(fileName);
                var lgroup = lRoot.Group("/");
                var ldataset = lgroup.Dataset("sig");

                var istart = (ulong)i * chunkSizePerChannel;
                var iend = istart + chunkSizePerChannel - 1;
                if (iend > nbdatapoints)
                    iend = nbdatapoints - 1;
                var chunkresult = Read_OneElectrodeDataAsInt(ldataset, electrodeProperties.Channel, istart, iend);
                Array.Copy(chunkresult, 0, result, (int)istart, (int)(iend - istart + 1));
                lRoot.Dispose();
            });

            return result;
        }

        public ushort[] Read_OneElectrodeDataAsInt(H5Dataset dataset, int channel, ulong startsAt, ulong endsAt)
        {
            var nbPointsRequested = endsAt - startsAt + 1;

           var datasetSelection = new HyperslabSelection(
                rank: 2,
                starts: new[] { (ulong)channel, startsAt },         // start at row ElectrodeNumber, column 0
                strides: new ulong[] { 1, 1 },                      // don't skip anything
                counts: new ulong[] { 1, nbPointsRequested },       // read 1 row, ndatapoints columns
                blocks: new ulong[] { 1, 1 }                        // blocks are single elements
            );

            var memorySelection = new HyperslabSelection(
                rank: 1,
                starts: new ulong[] { 0 },
                strides: new ulong[] { 1 },
                counts: new[] { nbPointsRequested },
                blocks: new ulong[] { 1 }
            );

            var memoryDims = new[] { nbPointsRequested };

            var result = dataset
                .Read<ushort>(
                    fileSelection: datasetSelection,
                    memorySelection: memorySelection,
                    memoryDims: memoryDims
                );

            return result;
        }

    }
}

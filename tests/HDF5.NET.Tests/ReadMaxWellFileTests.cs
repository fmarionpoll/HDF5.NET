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
            double YuM);

        [Fact]
        public void OpenAndReadH5MaxwellFileTest()
        {
           // var localFilePath = "E:\\2021 MaxWell\\Trace_20210715_16_54_48_1mM(+++).raw.h5";          //HDD
            var localFilePath = "C:\\Users\\fred\\Downloads\\Trace_20210715_16_54_48_1mM(+++).raw.h5"; //SSD
            if (!OpenReadMaxWellFile(localFilePath))
                throw new Exception();

            var electrodeProperties = new ElectrodeProperties(0, 683, 0, 0);
            var unused = ReadAll_OneElectrodeAsInt(electrodeProperties);
            //var result2 = ReadAll_OneElectrodeAsIntParallel(electrodeProperties);
        }

        public bool OpenReadMaxWellFile(string fileName)
        {
            Root = H5File.OpenRead(fileName);
            return Root != null;
        }

        public ushort[] ReadAll_OneElectrodeAsInt(ElectrodeProperties electrodeProperties)
        {
            H5Group group = Root.Group("/");
            H5Dataset dataset = group.Dataset("sig");
            int ndimensions = dataset.Space.Rank;
            if (ndimensions != 2)
                return null;
            var nbdatapoints = dataset.Space.Dimensions[1];
            return Read_OneElectrodeDataAsInt(dataset, electrodeProperties.Channel, 0, nbdatapoints - 1);
        }

        public ushort[] ReadAll_OneElectrodeAsIntParallel(ElectrodeProperties electrodeProperties)
        {
            H5Group group = Root.Group("/");
            H5Dataset dataset = group.Dataset("sig");
            var nbdatapoints = dataset.Space.Dimensions[1];      // any size*
            const ulong chunkSizePerChannel = 200;
            var result = new ushort[nbdatapoints];
            var nchunks = (long)(nbdatapoints / chunkSizePerChannel);

            int ndimensions = dataset.Space.Rank;
            if (ndimensions != 2)
                return null;

            Parallel.For(0, nchunks, i =>
            {
                var istart = (ulong)i * chunkSizePerChannel;
                var iend = istart + chunkSizePerChannel - 1;
                if (iend > nbdatapoints)
                    iend = nbdatapoints - 1;
                var chunkresult = Read_OneElectrodeDataAsInt(dataset, electrodeProperties.Channel, istart, iend);
                Array.Copy(chunkresult, 0, result, (int)istart, (int)(iend - istart + 1));
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

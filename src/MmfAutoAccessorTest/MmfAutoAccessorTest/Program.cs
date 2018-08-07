using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.MemoryMappedFiles;
using System.IO;
using System.Diagnostics;

namespace MmfAutoAccessorTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = "largefile.dat";
            var mmfSize = 1024*1024*500; //500M bytes
            const long chunkSize = 1024*1024*10; //chunk size 10M bytes
            var data = Encoding.Default.GetBytes("hello,world");

            MemoryMappedFile mmf = null;
            MmfAutoAccessor mmfAccessor = null;
            try
            {
                mmf = MemoryMappedFile.CreateFromFile(path, FileMode.OpenOrCreate, "mmftest", mmfSize, MemoryMappedFileAccess.ReadWrite);

                //use random view accessor: MemoryMappedFileViewAccessor
                mmfAccessor = new MmfAutoAccessor(mmf, mmfSize, chunkSize);

                var wCnt = mmfAccessor.Write(1024, data, data.Length);

                byte[] rData;
                var rCnt = mmfAccessor.Read(1024, out rData, data.Length);

                Console.WriteLine(Encoding.Default.GetString(rData));

                mmfAccessor.WriteByte(0, 0x01);
                var value = mmfAccessor.ReadByte(0);
                Debug.Assert(value == 0x01);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                try
                {
                    //call dispose
                    if (mmfAccessor != null)
                        mmfAccessor.Dispose();
                    if (mmf != null)
                        mmf.Dispose();

                    if (File.Exists(path))
                        File.Delete(path);
                }
                catch { }
            }
        }
    }
}

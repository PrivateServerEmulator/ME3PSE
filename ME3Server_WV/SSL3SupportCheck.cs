using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ME3Server_WV
{
    public class SSL3SupportCheck
    {
        [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
        private static extern int BCryptAddContextFunction(ConfigurationTable dwTable, string pszContext, CryptographicInterface dwInterface, string pszFunction, FunctionPosition dwPosition);

        [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
        private static extern int BCryptRemoveContextFunction(ConfigurationTable dwTable, string pszContext, CryptographicInterface dwInterface, string pszFunction);

        [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
        private static extern int BCryptEnumContextFunctions(ConfigurationTable dwTable, string pszContext, CryptographicInterface dwInterface, ref uint pcbBuffer, out IntPtr ppBuffer);

        [DllImport("bcrypt.dll")]
        private static extern void BCryptFreeBuffer(IntPtr pvBuffer);

        private static readonly string ssl3serverpath = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 3.0\Server";

        private enum FunctionPosition : uint
        {
            CRYPT_PRIORITY_TOP = 0x00000000,
            CRYPT_PRIORITY_BOTTOM = 0xFFFFFFFF
        }

        private enum CryptographicInterface : uint
        {
            BCRYPT_ASYMMETRIC_ENCRYPTION_INTERFACE = 0x00000003,
            BCRYPT_CIPHER_INTERFACE = 0x00000001,
            BCRYPT_HASH_INTERFACE = 0x00000002,
            BCRYPT_RNG_INTERFACE = 0x00000006,
            BCRYPT_SECRET_AGREEMENT_INTERFACE = 0x00000004,
            BCRYPT_SIGNATURE_INTERFACE = 0x00000005,
            NCRYPT_KEY_STORAGE_INTERFACE = 0x00010001,
            NCRYPT_SCHANNEL_INTERFACE = 0x00010002,
            NCRYPT_SCHANNEL_SIGNATURE_INTERFACE = 0x00010003
        }

        private enum ConfigurationTable : uint
        {
            CRYPT_LOCAL = 0x00000001,
            CRYPT_DOMAIN = 0x00000002
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct CRYPT_CONTEXT_FUNCTIONS
        {
            public int cFunctions;
            public IntPtr rgpszFunctions;
        }

        private static List<string> GetCipherSuiteList()
        {
            var res = new List<string>();
            uint size = 0;
            BCryptEnumContextFunctions(ConfigurationTable.CRYPT_LOCAL, "SSL", CryptographicInterface.NCRYPT_SCHANNEL_INTERFACE, ref size, out IntPtr ptrBuffer);
            CRYPT_CONTEXT_FUNCTIONS ccf = (CRYPT_CONTEXT_FUNCTIONS)Marshal.PtrToStructure(ptrBuffer, typeof(CRYPT_CONTEXT_FUNCTIONS));
            for (int i = 0; i < ccf.cFunctions; i++)
            {
                IntPtr p = Marshal.ReadIntPtr(ccf.rgpszFunctions + (IntPtr.Size * i));
                string s = Marshal.PtrToStringUni(p);
                res.Add(s);
            }
            BCryptFreeBuffer(ptrBuffer);
            return res;
        }

        private static bool AddCipherSuite(string strCipherSuite, bool top = false)
        {
            var x = BCryptAddContextFunction(ConfigurationTable.CRYPT_LOCAL, "SSL", CryptographicInterface.NCRYPT_SCHANNEL_INTERFACE,
                strCipherSuite, top ? FunctionPosition.CRYPT_PRIORITY_TOP : FunctionPosition.CRYPT_PRIORITY_BOTTOM);
            return x == 0;
        }

        private static int RemoveCipherSuite(string strCipherSuite)
        {
            return BCryptRemoveContextFunction(ConfigurationTable.CRYPT_LOCAL, "SSL", CryptographicInterface.NCRYPT_SCHANNEL_INTERFACE, strCipherSuite);
        }

        public static bool CheckCipherSuites()
        {
            var list = GetCipherSuiteList();
            return list.Contains("TLS_RSA_WITH_RC4_128_SHA") || list.Contains("TLS_RSA_WITH_RC4_128_MD5");
        }

        public static bool EnableCipherSuites()
        {
            var a = AddCipherSuite("TLS_RSA_WITH_RC4_128_SHA");
            var b = AddCipherSuite("TLS_RSA_WITH_RC4_128_MD5");
            return a || b;
        }

        public static int GetSSL3ServerStatus()
        {
            object result = Registry.GetValue(ssl3serverpath, "Enabled", -1);
            if (result == null)
                return -2;
            return (int)result;
        }

        public static bool EnableSSL3Server()
        {
            try
            {
                Registry.SetValue(ssl3serverpath, "Enabled", 1, RegistryValueKind.DWord);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Log("[EnableSSL3Server]\n" + ME3Server.GetExceptionMessage(ex), Color.Red);
                return false;
            }
        }

    }
}

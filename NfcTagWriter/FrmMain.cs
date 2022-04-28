using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NfcDevice;

namespace NfcTagWriter
{
    public partial class FrmMain : Form
    {
        private ACR122U acr122u = new ACR122U();

        public FrmMain()
        {
            InitializeComponent();
        }

        private byte[] GetSHA1(byte[] data)
        {
            byte[] hash = null;

            using (SHA1 sha1 = SHA1.Create())
            {
                hash = sha1.ComputeHash(data);
            }

            return hash;
        }

        private byte[] CreateNfcData(byte[] data)
        {
            // length of data (4 bytes) + data + sha1 (20 bytes)
            List<byte> nfcData = new List<byte>();
            nfcData.AddRange(BitConverter.GetBytes(data.Length));
            nfcData.AddRange(data);
            nfcData.AddRange(GetSHA1(data));

            return nfcData.ToArray();
        }

        private byte[] GetData(byte[] nfcData)
        {
            byte[] data = new byte[BitConverter.ToInt32(nfcData, 0)];
            byte[] sha1 = new byte[20];

            Array.Copy(nfcData, 4, data, 0, data.Length);
            Array.Copy(nfcData, data.Length + 4, sha1, 0, sha1.Length);

            if (GetSHA1(data).SequenceEqual(sha1))
                return data;
            else
                return null;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            TxtData.MaxLength = 38;

            acr122u.Init(false, (TxtData.MaxLength * 2) + 20 + 4, 4, 4, 100);  // MaxLength * 2 if using UTF8 + 20 bytes SHA1 + 4 bytes length of data
            acr122u.CardInserted += Acr122u_CardInserted;
            acr122u.CardRemoved += Acr122u_CardRemoved;
        }

        private void Acr122u_CardInserted(PCSC.ICardReader reader)
        {
            Invoke((MethodInvoker)delegate ()
            {
                LblResult.ForeColor = Color.Black;
                LblResult.Text = "...";
            });

            try
            {
                if (ChkRead.Checked)
                {
                    byte[] data = GetData(acr122u.ReadData(reader));

                    Invoke((MethodInvoker)delegate ()
                    {
                        if (data != null)
                        {
                            LblResult.ForeColor = Color.Green;
                            LblResult.Text = "OK";

                            TxtData.Text = Encoding.UTF8.GetString(data);
                        }
                        else
                        {
                            LblResult.ForeColor = Color.Red;
                            LblResult.Text = "ERROR";

                            TxtData.Text = string.Empty;
                        }
                    });
                }
                else
                {
                    bool ret = acr122u.WriteData(reader, CreateNfcData(Encoding.UTF8.GetBytes(TxtData.Text)));

                    Invoke((MethodInvoker)delegate ()
                    {
                        if (ret)
                        {
                            LblResult.ForeColor = Color.Green;
                            LblResult.Text = "OK";
                        }
                        else
                        {
                            LblResult.ForeColor = Color.Red;
                            LblResult.Text = "ERROR";
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Invoke((MethodInvoker)delegate ()
                {
                    LblResult.ForeColor = Color.Red;
                    LblResult.Text = ex.Message;
                });
            }
        }

        private void Acr122u_CardRemoved()
        {
            Invoke((MethodInvoker)delegate ()
            {
                TxtData.Text = string.Empty;
                LblResult.Text = string.Empty;
            });
        }
    }
}

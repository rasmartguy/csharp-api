using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.IO;
using RAPIlib;

namespace test_prj
{
    public partial class Form1 : Form
    {
        private string mykeystr = "";
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ulong bl = 0;
            ulong trn = 0;
            ulong bin = 0;
            try
            {
                using (RAPIlib.Connector node= new Connector())
                {
                    if (node.cmdGetCounters(ref bl,ref trn,ref bin))
                    {
                        textBox1.Text = String.Format("node addr {0}; port {1} | {2}: {3} \\  {4}",
                            ConfigManager.node_addr, ConfigManager.node_port, bl, trn, bin);
                    }
                }
            }
            catch (System.Exception ex)
            {

                textBox1.Text = "No info received";
                MessageBox.Show(string.Format("[{0}]:{1}", ex.HResult, ex.Message), "RAPIlib ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {

            try
            {
                grid.Rows.Clear();
                using (RAPIlib.Connector node= new Connector())
                {
                    List<HashType> list = node.cmdGetBlocks();
                    if (list.Count == 0)
                    {
                        textBox2.Text = "No blocks returned!";
                    }
                    else
                    {
                        textBox2.Text = String.Format("returned {0} blocks:\r\n", list.Count);
                        int n = 1;
                        foreach (HashType itm in list)
                        {
                            uint trns = 0;
                            uint bins = 0;
                            if (node.cmdGetBlockSize(itm, ref trns, ref bins))
                            {
                                grid.Rows.Add(new string[] { n.ToString(), itm.ToString(), trns.ToString(), bins.ToString() });
                                //textBox2.Text += String.Format("{0} : [{1} . . . {2}]: !! data not received!!\r\n", n++, itm.ToString().Substring(0, 8)
                                //  , itm.ToString().Substring(itm.ToString().Length - 8, 8));
                            }
                            else
                                grid.Rows.Add(new string[] { n.ToString(), itm.ToString(), "not recv", "not recv" });
                            //textBox2.Text += String.Format("{0} : [{1} . . . {2}]: transactions {3}; binaries {4}\r\n", n++, itm.ToString().Substring(0, 8)
                            //  , itm.ToString().Substring(itm.ToString().Length - 8, 8), trns, bins);
                            n++;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {

                MessageBox.Show(string.Format("[{0}]:{1}", ex.HResult, ex.Message), "RAPIlib ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            //string hname = "C65C320D3B3A12772390729BC3C3D80BD90C6DC212C2F13A76BB1745DD84E341C0AAB73559D19A16F4B80A2013145B6B107BBB17CE08D6FEE6163AE28AC4F443";
            string hname = "A53257D683D8ABFE85961C5575A072518C36ED16BB408A27C9073F0DB1DC615844909A4E699131CD6747F1B16CA2FC1CE6B1D0896560D3129EC9778BB026B63C";
            HashType h = new HashType();
            h.FromString(hname);
            // string tmpfile = Path.GetTempFileName();
            try
            {
                using (RAPIlib.Connector node= new Connector())
                {
                    h = node.cmdGetLastHash();
                    //byte[] buf = node.cmdGetBinaryPart(h,0,0,75131);
                    byte[] buf = node.cmdGetBinaryData(h);
                    if (buf.Length != 0)
                    {
                        string tmpfile = Path.GetTempFileName();
                        File.WriteAllBytes(tmpfile, buf);
                        Form2 f = new Form2();
                        f.Show();
                        f.pictureBox1.ImageLocation = tmpfile;
                        try
                        {
                            f.pictureBox1.Load();
                        }
                        catch (System.Exception ex)
                        {

                            Graphics gr = f.pictureBox1.CreateGraphics();
                            gr.DrawString(string.Format("error loading image:\r\n{0}", ex.Message), new Font("Arial", 24), new SolidBrush(Color.Black), new PointF(150.0F, 150.0F));
                            return;
                        }
                        //f.pictureBox1.ImageLocation = "";

                    }
                    else
                        throw new System.Exception("block not contained binary data");
                }
            }
            catch (System.Exception ex)
            {

                MessageBox.Show(string.Format("[{0}]:{1}", ex.HResult, ex.Message), "RAPIlib ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

        }

        private void button4_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() != DialogResult.OK) return;
            byte[] buff = File.ReadAllBytes(openFileDialog1.FileName);
            HashType r1;
            bool good = false;
            using (RAPIlib.Connector node= new Connector())
            {
                r1 = node.cmdGetLastHash();
                good = node.cmdCommitBinaryData(buff);
            }
            Thread.Sleep(500);
            using (RAPIlib.Connector node= new Connector())
            {
                if (good)
                {

                    good = false;
                    do
                    {
                        HashType r2 = node.cmdGetLastHash();
                        if (!r1.Equals(r2))
                        {
                            good = true;
                            buff = node.cmdGetBinaryData(r2);
                            if (buff.Length != 0)
                            {
                                string tmpfile = Path.GetTempFileName();
                                File.WriteAllBytes(tmpfile, buff);
                                Form2 f = new Form2();
                                f.Show();
                                f.pictureBox1.ImageLocation = tmpfile;
                                try
                                {
                                    f.pictureBox1.Load();
                                }
                                catch (System.Exception ex)
                                {
                                    Graphics gr = f.pictureBox1.CreateGraphics();
                                    gr.DrawString(string.Format("error loading image:\r\n{0}",ex.Message), new Font("Arial", 24), new SolidBrush(Color.Black), new PointF(150.0F, 150.0F));
                                    f.pictureBox1.Refresh();
                                }
                                return;
                                //f.pictureBox1.ImageLocation = "";

                            }
                            else
                            {
                                MessageBox.Show("данные не получены с ноды");
                                return;
                            }
                        } 
                    } while (!good);
                   
                    
                }
                MessageBox.Show("данные не отправились");
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            using (RAPIlib.Connector node= new Connector())
            {
                KeyType my_key = new KeyType();
                if (MessageBox.Show("Load you public key?","",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes)
                {
                    openFileDialog1.FileName = "";
                    if (openFileDialog1.ShowDialog()==DialogResult.OK)
                    {
                        byte[] kk = File.ReadAllBytes(openFileDialog1.FileName);
                        kk.CopyTo(my_key.data, 0);
                        mykeystr = my_key.ToString();
                    }
                }
                KeyType k = node.cmdGetInfo(my_key);
                MessageBox.Show(string.Format("{0}\r\n{1}",k.ToString(),mykeystr));

            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            string blstr = "0EBE78BD45F1132A0519DE10941D9EF891259AC8D99709FC71EE0B312FEC30E108A85459816138C4AE90E56DCE69A1457FAB870B4186CA8FF3B74D9E6004C9AA";
            //blstr = "2741B2E98188EC7DDB42B42048FC527BE65BA2949537252D7BD468D82751985EA4AE0CD09D72FFD8A2FFFF553ADC3413A08B1A488529F0442FD66D722DE0C5A9";
            blstr = "A7DA4EFA7637FC5F816C6233F59CADE40064C664313DEF044D192F71DF8FC9CB256062207ECD23CDD1C0585DB0490520BE72531FBC584F0864F3350811132535";
            HashType bl = new HashType();
            bl.FromString(blstr);
            List<HashType> lst=new List<HashType>();
            using (RAPIlib.Connector node= new Connector())
            {
                bl = node.cmdGetLastHash();
                lst = node.cmdGetTransactions(bl, 0, 1);
                if (lst.Count==0)
                {
                    MessageBox.Show("no returned transactions");
                } else
                {
                    string ret = string.Format("returned {0} transactions:\r\n", lst.Count);
                    foreach (HashType i in lst)
                    {
                        ret += string.Format("{0}\r\n", i.ToString());
                    }
                    //MessageBox.Show(ret);
                }
            }
            if (lst.Count>0)
            {
                using (RAPIlib.Connector node = new Connector())
                {
                    Transaction tr = node.cmdGetTransaction(bl, lst[0]);
                    string res = string.Format("sign: {0}\r\nsender: {1}\r\nreceiver: {2}\r\namount: {3}\\{4}\r\ncurrency: {5}",
                        tr.signature.ToString(), tr.sender.ToString(),
                        tr.receiver.ToString(), tr.amount_high.ToString(), tr.amount_low.ToString(), tr.currency);
                    MessageBox.Show(res);
                } 
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            //string pub = "C:\\Users\\User\\.bigdb\\.KEYS\\public.key";
            //string priv = "C:\\Users\\User\\.bigdb\\.KEYS\\private.key";
            //string spubstr = "53C0AECECE0A3D9E2E69230C977E874C8DDA4A2FE1F162AF98979E69BC27F859";
            //string rpubstr = "7CFC0A75AEA16D99E09F5F9FF05C5A698BA27C52A02782BDB7A4687953BBE154";
            KeyType send_pub = new KeyType();
            HashType send_priv = new HashType();
            KeyType recv_pub = new KeyType();
            HashType recv_priv = new HashType();
            UInt32 startsumm = 1000000000;
            UInt32 degree = 5000;
            while (MessageBox.Show("append new transaction?", "Q", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                ConfigManager.noconnectflg = true;
                if (recv_pub.empty())                
                { 
                    using (Connector node = new Connector())
                    {
                       if (!( node.GenKeyPair(ref send_pub, ref send_priv)))
                        {
                            MessageBox.Show("Do not generated key pair 1 ! ABORT KORABLYA");
                            break;
                        }
                        if (!node.GenKeyPair(ref recv_pub, ref recv_priv))                            
                            {
                                MessageBox.Show("Do not generated key pair3 ! ABORT KORABLYA");
                                break;
                            }
                    }
                } else
                {
                    send_pub.Apply(recv_pub);
                    send_priv.Apply(recv_priv);
                    using (Connector node = new Connector())
                    {
                        if (!node.GenKeyPair(ref recv_pub,ref recv_priv))
                        {
                            MessageBox.Show("Do not genegated receiver key pair! ABORT KORABLYA");
                            break;
                        }
                    }
                }
                ConfigManager.init(send_pub, send_priv);
                using (Connector node=new Connector())
                {
                    //TODO: смоделировать ошибочную посылку транзакции
                    node.cmdCommitTransaction(send_pub, recv_pub, send_priv, startsumm, 0);
                    MessageBox.Show(string.Format("trnsaction Amount ({0}\\{1} applied\r\nsender:{2}\r\nrecever:{3}",
                        startsumm, 0, send_pub.ToString(), recv_pub.ToString()));
                    //node.cmdCommitTransaction(send_pub, recv_pub, send_priv, startsumm, 0); // errorr!!!
                    startsumm -= degree;                    
                }
                if (MessageBox.Show("get receiver balance?", "Q", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    ConfigManager.init(recv_pub, recv_priv);
                    using (Connector node = new Connector())
                    {
                        Amount res = node.cmdGetBalance();
                        MessageBox.Show(String.Format("wallet {0}\r\n balance returned:\r\nhigh: {1}, low: {2}", recv_pub.ToString(), res.hight, res.low), "BALANCE");
                    }
                }
            }
            //    ConfigManager.init(pub, priv);
            //if (MessageBox.Show("insert new transaction?","Q",MessageBoxButtons.YesNo,MessageBoxIcon.Question)==DialogResult.Yes)
            //{
            //    using (Connector node = new Connector())
            //    {
            //        KeyType send_pub = new KeyType();
            //        send_pub.FromString(spubstr.Substring(0, 64));
            //        KeyType recv_pub = new KeyType();
            //        recv_pub.FromString(rpubstr.Substring(0, 64));
            //        recv_pub.data.CopyTo(ConfigManager.my_pubkey, 0);
            //        node.cmdCommitTransaction( recv_pub, send_pub, 5000, 0);
            //        //List<Transaction> lst = node.cmdGetTransactionsByKey(0, 10);
            //        //MessageBox.Show(string.Format("returned {0} transactions", lst.Count));

            //    } 
            //}
            //if (MessageBox.Show("get receiver balance?", "Q", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            //{
            //    using (Connector node = new Connector())
            //    {
            //        //string newkey = "7CFC0A75AEA16D99E09F5F9FF05C5A698BA27C52A02782BDB7A4687953BBE154";
            //        KeyType me = new KeyType();
            //        me.FromString(rpubstr);
            //        me.data.CopyTo(ConfigManager.my_pubkey, 0);
            //        Amount res = node.cmdGetBalance();
            //        MessageBox.Show(String.Format("wallet {0}\r\n balance returned:\r\nhigh: {1}, low: {2}", me.ToString(), res.hight, res.low), "BALANCE");
            //    } 
            //}
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string pub = "C:\\Users\\User\\.db1\\.KEYS\\public.key";
            string priv = "C:\\Users\\User\\.db1\\.KEYS\\private.key";
            string ipaddr = "10.0.0.61";
            KeyType pubkey = new KeyType();
            HashType privkey = new HashType();
            int ipport = 38100;
                if (File.Exists(pub))
                    pubkey.Apply(File.ReadAllBytes(pub));
                if (File.Exists(priv))
                    privkey.Apply( File.ReadAllBytes(priv));
            //}
            //ConfigManager.init(pub, priv);
            ConfigManager.init(pubkey,privkey,ipaddr, ipport);
            
            Connector node;
            string result = "";
            KeyType key = new KeyType();
            try
            {
                using (node = new Connector())
                {

                    
                        ConfigManager.my_pubkey.CopyTo(key.data, 0);
                        if (node.lasterrcode==0) key = node.cmdGetInfo(key);             
                    

                }
                ulong bl = 0;
                ulong trn = 0;
                ulong bin = 0;
                using (node=new Connector())
                {

                    if (node.lasterrcode == 0) node.cmdGetCounters(ref bl, ref trn, ref bin);
                    
                }
                if (node.lasterrcode == 0)
                    result = string.Format("pub key: {0}\r\nnode: {1}:{2}; Counters: {3} | {4} | {5}", key.ToString(), ConfigManager.node_addr,
                        ConfigManager.node_port.ToString(), bl.ToString(), trn, bin);
                else
                    result = string.Format("error width connect to node {0}:{1}\r\n {2}: {3}", ConfigManager.node_addr,
                        ConfigManager.node_port, node.lasterrcode, node.lasterrmsg);

            }
            catch (System.Exception ex)
            {
                result = String.Format("Sys error: {0}", ex.Message);
                MessageBox.Show(ex.Message, "ERROR!");

            }
            textBox1.Text = result;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            KeyType pub = new KeyType();
            HashType priv = new HashType();
            ConfigManager.noconnectflg = true;
            using (Connector node=new Connector())
            {
                if (node.GenKeyPair(ref pub, ref priv))
                    MessageBox.Show(string.Format("Key pair generated\r\npub key:\r\n{0}\r\npriv key:\r\n{1}",
                        pub.ToString(), priv.ToString()));
                else
                    MessageBox.Show("Key pair do not been generated");
            }
        }
    }
}

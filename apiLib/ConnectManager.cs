using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.IO;


namespace RAPIlib
{
    /// <summary>
    /// save actual parametrs of connection: ip-address; ip-port; public and private keys (as byte array)
    /// </summary>
    public static class ConfigManager
    {
        /// <summary>
        /// inits params of connection 
        /// </summary>
        /// <param name="ipaddr">ip address</param>
        /// <param name="ipport">ip port</param>
        /// <param name="noconnect">if sets true, instance of Connector will not do attempt of connection.</param>
        public static void init(string ipaddr,int ipport, bool noconnect=false)
        {
            node_addr = ipaddr;
            node_port = ipport;
            noconnectflg = noconnect;
        }
        /// <summary>
        /// init params of connection 
        /// </summary>
        /// <param name="my_pub">public key</param>
        /// <param name="my_priv">private key</param>
        /// <param name="ipaddr">ip address</param>
        /// <param name="ipport">ip port</param>
        /// <param name="noconnect">if set true, instance of Connector will not do attempt of connection</param>
        public static void init(KeyType my_pub=null,HashType my_priv=null,string ipaddr="",int ipport=-1,bool noconnect=false)
        {
            if (my_pub!=null) my_pubkey = my_pub.data;
           if (my_priv!=null) my_privkey = my_priv.data;
            if (ipaddr != "")
                node_addr = ipaddr;
            if (ipport != -1)
                node_port = ipport;
            noconnectflg = noconnect;
        }
        public static byte[] my_pubkey;
        public static byte[] my_privkey;
        public static string node_addr;
        public static int node_port;
        /// <summary>
        /// if == true instance of Connector will not do attempt of connection
        /// </summary>
        public static bool noconnectflg;

    }
    /// <summary>
    /// provide ip-connection with node and send\recv commands RA_API
    /// </summary>
    public class Connector :IDisposable
    {

        private int i_errcode=0;
        private string i_errmsg = "";
        /// <summary>
        /// code of last error
        /// </summary>
        public int lasterrcode { get { return i_errcode; } }
        /// <summary>
        /// messgage of work error
        /// </summary>
        public string lasterrmsg { get { return i_errmsg; } }
        private Socket sock;
        /// <summary>
        /// default Constructor, connection's params get from  ConfigManager
        /// </summary>
        public Connector() {  sock = new Socket(SocketType.Stream, ProtocolType.IP);
            if (!ConfigManager.noconnectflg)
            {
                try
                {
                    sock.Connect(ConfigManager.node_addr, ConfigManager.node_port);
                }
                catch (System.Exception ex)
                {
                    i_errcode = ex.HResult;
                    i_errmsg = ex.Message;                    
                } 
            }
        }
        /// <summary>
        /// Constructor with ip, port
        /// </summary>
        /// <param name="iaddr">ip-address</param>
        /// <param name="iport">ip-port</param>
        public Connector(string iaddr,int iport) { 
            ConfigManager.init(iaddr, iport);
            sock = new Socket(SocketType.Stream, ProtocolType.IP);
            if (!ConfigManager.noconnectflg)
            {
                try
                {
                    sock.Connect(ConfigManager.node_addr, ConfigManager.node_port);
                }
                catch (System.Exception ex)
                {
                    i_errcode = ex.HResult;
                    i_errmsg = ex.Message;
                    
                } 
            }
        }
        ~Connector()
        {
            if (sock.Connected) sock.Shutdown(SocketShutdown.Both);
            sock.Dispose();
            GC.Collect();
        }
        /// <summary>
        /// calculate crc sum of binary buffer ,
        /// use calc_crs_bin_data from ra_lib.dll
        /// </summary>
        /// <param name="buf">binary buffer</param>
        /// <returns></returns>        /// 
        public unsafe UInt64 Calc_CRC_binary(byte[] buf)
        {
            
            uint sz = (uint)buf.Length;
            fixed (byte *buf_ptr=&buf[0])
            {
                return calc_crs_bin_data(buf_ptr, sz);
            }
             
        }
        [DllImport("ra_lib.dll")]
        private unsafe static extern UInt64 calc_crs_bin_data(byte* data, uint data_sz);
        [DllImport("ra_lib.dll")]
        private unsafe static extern bool sign(  byte * data,
                     ulong data_sz,
                       byte * public_key,
                     ulong public_key_sz,
                       byte*  private_key,
                     ulong private_key_sz,
                        byte*  signature,
                     ulong signature_sz,
                      char* Status=null ,
                     ulong StatusSz = 0);
        [DllImport("ra_lib.dll")]
        private unsafe static extern bool verify(byte* data,
            ulong data_sz,
            byte* public_key,
            ulong public_key_sz,
            byte* signature,
            ulong signature_sz,
            char* Status = null,
            ulong StatusSz = 0);
        [DllImport("ra_lib.dll")]
        private unsafe static extern bool gen_keys_pair(byte* public_key_buffer,
                   ulong public_key_sz,
                   byte* private_key_buffer,
                   ulong private_key_sz,
                   char* Status = null,
                   ulong StatusSz = 0);
        /// <summary>
        /// generate signature for transaction, using private and public keys, function from ra_lib.dll
        /// </summary>
        /// <param name="data">transaction's data</param>
        /// <param name="pubkey">public key</param>
        /// <param name="privkey">private key</param>
        /// <returns>signature 64byte lenght binary buffer</returns>
        public unsafe byte[] gen_signature(byte[] data,KeyType pubkey,HashType privkey)
        {
            byte[] result = new byte[64];
            bool r = false;
            ulong data_sz = (ulong)Encoding.ASCII.GetCharCount(data);
            ulong pubkey_sz = (ulong)Encoding.ASCII.GetCharCount(pubkey.data);
            ulong privkey_sz = (ulong)Encoding.ASCII.GetCharCount(privkey.data);
            
            fixed (byte* datachr = &data[0], pubchr = &pubkey.data[0],
                privchr = &privkey.data[0])                 
            {
                byte[] s = new byte[64];
                fixed (byte *s_ptr=&s[0]) {
                    try
                    {
                        r = sign(datachr, data_sz, pubchr, pubkey_sz, privchr, privkey_sz,  s_ptr, (ulong)s.Length);
                        if (!verify(datachr,data_sz,pubchr,pubkey_sz,s_ptr,  (ulong)s.Length))
                        {
                            r = false;                            
                        }
                        if (r)
                        {
                            //byte[] bb = Encoding.BigEndianUnicode.GetBytes(s, 0, 32);
                            s.CopyTo(result, 0);
                        }
                    }
                    catch (System.Exception ex)
                    {

                        throw;
                    }
                }                
                
            }
            if (r)
                return result;
            else
                return null;
        }
         /// <summary>
         /// generate keys functions from ra_lib.dll
         /// </summary>
         /// <param name="newpubkey"></param>
         /// <param name="newprivkey"></param>
         /// <returns></returns>         
        public unsafe bool GenKeyPair(ref KeyType newpubkey, ref HashType newprivkey )
        {
            bool ret = false;
            byte[] pub_buf = new byte[32];
            byte[] priv_buf = new byte[64];
            fixed (byte * pub_ptr=&pub_buf[0],priv_ptr=&priv_buf[0])
            {
                ret = gen_keys_pair(pub_ptr, 32, priv_ptr, 64);
                if (ret)
                {
                    pub_buf.CopyTo(newpubkey.data, 0);
                    priv_buf.CopyTo(newprivkey.data, 0);
                }
            }
            return ret;
        }
        private bool i_connect()
        {
            i_errcode = 0;
            i_errmsg = "";
            if (!sock.Connected)
            {
                try
                {
                    sock.Connect(ConfigManager.node_addr, ConfigManager.node_port);
                    return true;
                }
                catch (System.Exception ex)
                {
                    i_errcode = ex.HResult;
                    i_errmsg = ex.Message;                    
                    return false;
                }
            }
            if (!sock.Poll(2,SelectMode.SelectWrite))
            {
                i_errcode = -1;
                i_errmsg = "сокет не готов к записи";
            }
           
            else if (sock.Poll(2, SelectMode.SelectError))
            {
                i_errcode = -1;
                i_errmsg = "сокет в состоянии ошибки";
            }
            return i_errcode == 0 ? true : false;
        }
        /// <summary>
        /// get counters of blockchain from node 
        /// </summary>
        /// <param name="block_counter">blocks' value</param>
        /// <param name="tran_counter">transaction's value</param>
        /// <param name="bindata_counter">binary data's value</param>
        /// <remarks> corrent values returns only after reindex on the node </remarks>
        /// <returns>true - in succeful case </returns>
        public bool cmdGetCounters(ref ulong block_counter, ref ulong tran_counter, ref ulong bindata_counter)
        {
            if (i_connect())
            {
                ApiCmd cmd = new ApiCmd(ApiCommands.apiGetCounters);
                //cmd.cmd = ApiCommands.apiGetCounters;                
                sock.Send(cmd.GetBytes());
                cmd.cmd = ApiCommands.apiTerminatingBlock;
                sock.Send(cmd.GetBytes());
                byte[] recv = new byte[6];
                sock.Receive(recv);
                cmd.SetFromBytes(recv);
                if (cmd.cmd==ApiCommands.apiSendCounters)
                {
                    recv = new byte[cmd.sz];
                    if (sock.Receive(recv) == recv.Length && recv.Length>0)
                    {
                        block_counter = BitConverter.ToUInt64(recv, 0);
                        tran_counter = BitConverter.ToUInt64(recv, 8);
                        bindata_counter = BitConverter.ToUInt64(recv, 16);
                        return true;
                    }
                    else
                        return false;
                } else
                    return true;
            }
            
            return false;
        }
        /// <summary>
        /// apiGetBlocks functions
        /// </summary>
        /// <param name="offset">смещение от конечно блока в цепочке</param>
        /// <param name="count">blocks' value</param>
        /// <returns>List[HashType] contains hashes(naems) of blocks</returns>
        public List<HashType> cmdGetBlocks(UInt64 offset=0,UInt16 count=20)
        {
            List<HashType> list = new List<HashType>();
            if (i_connect())
            {

                byte[] snd = new byte[sizeof(UInt64) + sizeof(UInt16)];
                BitConverter.GetBytes(offset).CopyTo(snd, 0);
                BitConverter.GetBytes(count).CopyTo(snd, 8);
                ApiCmd c = new ApiCmd(ApiCommands.apiGetBlocks);
                c.sz = (uint)snd.Length;
                sock.Send(c.GetBytes());
                sock.Send(snd);
                c.cmd = ApiCommands.apiTerminatingBlock;
                sock.Send(c.GetBytes());
                byte[] recv = new byte[6];
                sock.Receive(recv);
                c.SetFromBytes(recv);
                if (c.cmd==ApiCommands.apiSendBlocks)
                {
                    recv = new byte[c.sz];
                    int rbyte=sock.Receive(recv);
                    if (rbyte==c.sz)
                    {
                        for (int i=0; i<rbyte/64;i++)
                        {
                            HashType h = new HashType();
                            Array.Copy(recv, i * 64, h.data, 0, 64);
                            list.Add(h);
                        }

                    }
                }

            }
            return list;
        }
        /// <summary>
        /// cmdz apiGetBlockSize: content specified block
        /// </summary>
        /// <param name="block">hash(name) of block</param>
        /// <param name="tran_size">return value of transactions </param>
        /// <param name="bin_size">return value of binary data</param>
        /// <returns>true in success case
        </returns>
        public bool cmdGetBlockSize(HashType block, ref UInt32 tran_size,ref UInt32 bin_size)
        {
            if (i_connect())
            {
                ApiCmd c = new ApiCmd(ApiCommands.apiGetBlockSize);
                sock.Send(c.GetBytes());
                sock.Send(block.data);
                c.cmd = ApiCommands.apiTerminatingBlock;
                sock.Send(c.GetBytes());
                byte[] recv= new byte[6];
                sock.Receive(recv);
                c.SetFromBytes(recv);
                int repeat = 1;
                do
                {
                    if (c.cmd == ApiCommands.apiSendBlockSize)
                    {
                        repeat = 0;
                        recv = new byte[c.sz];
                        if (sock.Receive(recv) == c.sz)
                        {
                            tran_size = BitConverter.ToUInt32(recv, 64);
                            bin_size = BitConverter.ToUInt32(recv, 64 + sizeof(UInt32));
                            return true;
                        }
                        return false;
                    }
                    sock.Receive(recv);
                    c.SetFromBytes(recv);
                    repeat++;
                } while (repeat<10);
                return false;

            }
            else
                return false;
        }
        /// <summary>
        /// apiGetBinaryData returns binary record from specified block 
        /// </summary>
        /// <param name="block">block hash</param>
        /// <param name="offset">№ recocd into block</param>
        /// <returns>byte[] array - record</returns>
        public byte[] cmdGetBinaryData(HashType block,ushort offset=0)
        {
            ApiCmd c = new ApiCmd(ApiCommands.apiGetBinaryData);
            byte[] snd = new byte[c.sz];
            byte[] result=new byte[0];
            Array.Copy(block.data, snd, block.data.Length);
            Array.Copy(BitConverter.GetBytes(offset), 0, snd, block.data.Length, sizeof(ushort));
            if (i_connect())
            {
                sock.Send(c.GetBytes());
                sock.Send(snd);
                c.cmd = ApiCommands.apiTerminatingBlock;
                sock.Send(c.GetBytes());
                byte[] recv = new byte[6];
                sock.Receive(recv);
                c.SetFromBytes(recv);
                int repeat = 1;
                if (c.cmd == ApiCommands.apiGetBinaryData)
                {
                    if (c.sz > 0)
                    {
                        byte[] buf = new byte[c.sz];
                        
                        int t_recv = 0;
                        do
                        {
                            byte[] buf0 = new byte[c.sz];
                            int b_recv = sock.Receive(buf0);
                            if (b_recv != buf0.Length)
                                Array.Resize<byte>(ref buf0, b_recv);
                            try
                            {
                                buf0.CopyTo(buf, t_recv);
                            }
                            catch (Exception)
                            {
                                Array.Resize<byte>(ref buf0, buf.Length - t_recv);
                                buf0.CopyTo(buf, t_recv);
                               
                            }
                            t_recv += b_recv;
                        } while (t_recv < c.sz);
                        UInt64 crc = BitConverter.ToUInt64(buf, 0);
                        result = new byte[c.sz - sizeof(UInt64)];
                        Array.Copy(buf, sizeof(UInt64), result, 0, result.Length);
                        if (Calc_CRC_binary(result) != crc)
                        {
                            result = new byte[0];
                        }
                    }
                }
                //do
                //{
                //    if (c.cmd == ApiCommands.apiGetBinaryData)
                //    {

                    //        if (c.sz > 0)
                    //        {
                    //            byte[] buf = new byte[c.sz];
                    //            System.Threading.Thread.Sleep(10);
                    //            int b_sz = sock.Receive(buf);
                    //            if (b_sz == c.sz)
                    //            {
                    //                UInt64 crc = BitConverter.ToUInt64(buf, 0);
                    //                result = new byte[c.sz - sizeof(UInt64)];
                    //                Array.Copy(buf, sizeof(UInt64), result, 0, result.Length);
                    //                if (Calc_CRC_binary(result) == crc)
                    //                {
                    //                    repeat = 0;
                    //                    break;
                    //                }

                    //            }
                    //            else
                    //                repeat++;
                    //        }
                    //        else
                    //            break;
                    //    }
                    //    sock.Receive(recv);
                    //    c.SetFromBytes(recv);
                    //    repeat++;
                    //} while (repeat < 10);
            }
            return result;
        }
        /// <summary>
        /// apiGetBinaryPart return specified part of binary record from specified block 
        /// </summary>
        /// <param name="block">block hash</param>
        /// <param name="offset">№ number in block of record</param>
        /// <param name="offset_in">offset in bytes inside record </param>
        /// <param name="sz">length of returns part in bytes</param>
        /// <returns>byte[] array - return record's part </returns>
        public byte[] cmdGetBinaryPart(HashType block, ushort offset , uint offset_in , uint sz )
        {
            ApiCmd c = new ApiCmd(ApiCommands.apiGetBinaryPart);
            byte[] snd = new byte[c.sz];
            byte[] result = new byte[0];
            Array.Copy(block.data, snd, block.data.Length);
            Array.Copy(BitConverter.GetBytes(offset), 0, snd, block.data.Length, sizeof(ushort));
            Array.Copy(BitConverter.GetBytes(offset_in), 0, snd, block.data.Length+ sizeof(ushort), sizeof(uint));
            Array.Copy(BitConverter.GetBytes(sz), 0, snd, block.data.Length + sizeof(ushort) + sizeof(uint), sizeof(uint));
            if (i_connect())
            {
                sock.Send(c.GetBytes());
                sock.Send(snd);
                c.cmd = ApiCommands.apiTerminatingBlock;
                sock.Send(c.GetBytes());
                byte[] recv = new byte[6];
                sock.Receive(recv);
                c.SetFromBytes(recv);
                int repeat = 1;
                do
                {
                    if (c.cmd == ApiCommands.apiGetBinaryPart)
                    {
                        //if (c.sz >= sz)
                        //{
                            byte[] buf = new byte[c.sz];
                            if (sock.Receive(buf) == c.sz)
                            {
                                uint len = BitConverter.ToUInt32(buf, 64+sizeof(ushort)+sizeof(uint));
                                result = new byte[len];
                                Array.Copy(buf, 64 + sizeof(ushort) + sizeof(uint) + sizeof(uint), result, 0, (int)len);
                                repeat = 0;
                                break;
                            }

                        //}
                        //else
                        //    break;
                    }
                    sock.Receive(recv);
                    c.SetFromBytes(recv);
                    repeat++;
                } while (repeat < 10);
            }
            return result;
        }
        /// <summary>
        /// apiGetLastHash returns last hash from chain
        /// </summary>
        /// <returns>last hash</returns>
        public HashType cmdGetLastHash()
        {
            ApiCmd c = new ApiCmd(ApiCommands.apiGetLastHash);
            HashType result = new HashType();
            sock.Send(c.GetBytes());
            c.cmd = ApiCommands.apiTerminatingBlock;
            sock.Send(c.GetBytes());
            byte[] recv = new byte[6];
            sock.Receive(recv);
            c.SetFromBytes(recv);
            int repeat = 1;
            do
            {
                if (c.cmd == ApiCommands.apiSendLastHash)
                {
                    byte[] res = new byte[c.sz];
                    sock.Receive(res);
                    res.CopyTo(result.data, 0);
                    repeat = 0;
                    break;
                }
                else
                {
                    sock.Receive(recv);
                    c.SetFromBytes(recv);
                    repeat++;
                }
            } while (repeat<10);
            return result;

        }
        /// <summary>
        /// apiCommitBinaryData send binary data to node. With parametr sendterminate=false don't fininsh sending with termination block,
        /// what allows to send commands by queue.In this case client by end of queue should send cmdSendTerminate.
        /// </summary>
        /// <param name="buffer">data</param>
        /// <param name="check_resend">set/not set flag, while sending "check repeat send"</param>
        /// <param name="sendterminate">finish\not finish sending by command apiTerminatingBlock</param>
        /// <returns></returns>
        public bool cmdCommitBinaryData(byte[] buffer,bool check_resend=false, bool sendterminate=true)
        {
            ApiCmd c = new ApiCmd(ApiCommands.apiCommitBinaryData);
            c.sz =(uint) buffer.Length + 10;//+2 byte flag
            UInt64 crc = Calc_CRC_binary(buffer);
            byte[] snd = new byte[c.sz];
            BitConverter.GetBytes(crc).CopyTo(snd, 0);
            if (check_resend)
            {
                snd[9] = 1;
            }
            buffer.CopyTo(snd, 10);
            if (i_connect())
            {
                sock.Send(c.GetBytes());
                sock.Send(snd);
                //sock.Send(c.GetBytes());
                //sock.Send(snd);
                if (sendterminate)
                {
                    c.cmd = ApiCommands.apiTerminatingBlock;
                    sock.Send(c.GetBytes());
                    byte[] recv = new byte[6];
                    sock.Receive(recv);
                }
               
                return true;
            }
            else
                return false;

        }
        /// <summary>
        /// send Termination block
        /// </summary>
        /// <returns></returns>
        public bool cmdSendTerminating()
        {
            ApiCmd c = new ApiCmd(ApiCommands.apiTerminatingBlock);
            if (i_connect())
            {
                sock.Send(c.GetBytes());
                byte[] recv = new byte[6];
                sock.Receive(recv);
                return true;
            }
            else
                return false;
        }
        /// <summary>
        /// apiGetInfo 
        /// </summary>
        /// <param name="my_key">public key of a client </param>
        /// <returns>public key by the node</returns>
        public KeyType cmdGetInfo(KeyType my_key)
        {
            KeyType result = new KeyType();
            ApiCmd c = new ApiCmd(ApiCommands.apiGetInfo);
            if (i_connect())
            {
                sock.Send(c.GetBytes());
                sock.Send(my_key.data);
                c.cmd = ApiCommands.apiTerminatingBlock;
                sock.Send(c.GetBytes());

                byte[] recv = new byte[6];
                sock.Receive(recv);
                c.SetFromBytes(recv);
                if (c.cmd==ApiCommands.apiSendInfo)
                {
                    recv = new byte[c.sz];
                    sock.Receive(recv);
                    recv.CopyTo(result.data, 0);
                }
            }
            return result;
        }
        /// <summary>
        /// apiGetTransactions return list  UID(signatures) of transaction by a specified block
        /// </summary>
        /// <param name="blockid">block hash</param>
        /// <param name="offset">offset (transaction's valie) by start of a block</param>
        /// <param name="limit">value of transtions by send</param>
        /// <returns>singatures list(like HashType)</returns>
        /// <remarks>if limit more transaction's value in a block or limit set in node,will return max possible value of transactions</remarks>
        public List<HashType> cmdGetTransactions(HashType blockid,UInt64 offset,UInt16 limit)
        {
            List<HashType> result = new List<HashType>();
            ApiCmd c = new ApiCmd(ApiCommands.apiGetTransactions);
            byte[] snd = new byte[c.sz];
            blockid.data.CopyTo(snd, 0);
            BitConverter.GetBytes(offset).CopyTo(snd, 64);
            BitConverter.GetBytes(limit).CopyTo(snd, 72);
            if (i_connect())
            {
                sock.Send(c.GetBytes());
                sock.Send(snd);
                c.cmd = ApiCommands.apiTerminatingBlock;
                sock.Send(c.GetBytes());
                byte[] recvcmd = new byte[6];
                do
                {
                    sock.Receive(recvcmd);
                    c.SetFromBytes(recvcmd);
                    if (c.cmd == ApiCommands.apiSendTransactions)
                    {
                        byte[] recv = new byte[c.sz];
                        sock.Receive(recv);
                        for (int i = 0; i < (c.sz - 64) / 64; i++)
                        {
                            HashType h = new HashType();
                            Array.Copy(recv, 64 + (i * 64), h.data, 0, 64);
                            result.Add(h);
                        }
                        break;
                    } 
                } while (c.cmd !=ApiCommands.apiSendTransaction);
            }
            return result;
        }
        /// <summary>
        /// apiGetTransaction returns transaction fields by specified signarute by specified block
        /// </summary>
        /// <param name="blockid">block hash</param>
        /// <param name="trn_id">signarture/param>
        /// <returns>object typeof Transaction</returns>
        public Transaction cmdGetTransaction(HashType blockid,HashType trn_id)
        {
            Transaction tr = new Transaction();
            ApiCmd c = new ApiCmd(ApiCommands.apiGetTransaction);
            byte[] snd = new byte[128];
            blockid.data.CopyTo(snd, 0);
            Array.Copy(trn_id.data, 0, snd, 64, 64);
            if (i_connect())
            {
                sock.Send(c.GetBytes());
                sock.Send(snd);
                c.cmd = ApiCommands.apiTerminatingBlock;
                sock.Send(c.GetBytes());
                byte[] recvcmd = new byte[6];
                sock.Receive(recvcmd);
                c.SetFromBytes(recvcmd); 
                if (c.cmd==ApiCommands.apiSendTransaction)
                {
                    byte[] recv = new byte[c.sz];
                    sock.Receive(recv);
                    
                    Array.Copy(recv, 64+0, tr.signature.data, 0, 64);
                    Array.Copy(recv, 64 + 64, tr.sender.data, 0, 32);
                    Array.Copy(recv, 64 + 96, tr.receiver.data, 0, 32);
                    tr.amount_high = BitConverter.ToUInt32(recv, 64 + 128);
                    tr.amount_low = BitConverter.ToUInt64(recv, 64+132);
                    tr.currency = BitConverter.ToString(recv, 64 + 140, 16);
                    tr.currency = "";
                    int i = 204;
                    while (recv[i] != 0 && i < 220)
                    {
                        tr.currency += Convert.ToChar(recv[i++]);
                    }
                }
            }
            return tr;
        }
        /// <summary>
        /// send Transaction to nde. With parametr sendterminate=false don't finish sending with Termination Block,
        /// what allows to send commands by queue.In this case client by end of queue should send cmdSendTerminate.
        /// </summary>
        /// <param name="send_pub">public key "from whom(sender)"</param>
        /// <param name="recv_pub">public key "to whom(receiver)"</param>
        /// <param name="send_priv">private key "from whom(sender),requers to create signature</param>
        /// <param name="amount_high">integral part of amount</param>
        /// <param name="amount_low">fraction part of amount</param>
        /// <param name="currency">currency,default is "RAS"</param>
        /// <param name="sendterminate">send\not send TerminatingBlock</param>
        /// <remarks>if send_pub and\or send_priv are empty - values will be took from ConfigManager </remarks>
        public void cmdCommitTransaction(KeyType send_pub,KeyType recv_pub,HashType send_priv, UInt32 amount_high,UInt64 amount_low,string currency="RAS",bool sendterminate=true)
        {
            
            DateTime dt = DateTime.Now;
            Random rnd = new Random();
            //generate signature
            HashType prv = new HashType();
            Transaction tr = new Transaction();
            if (!send_pub.empty())
                tr.sender.Apply(send_pub);
            else
                tr.sender.Apply(ConfigManager.my_pubkey);
            if (!send_priv.empty())
                prv.Apply(send_priv);
            else
                prv.Apply(ConfigManager.my_privkey);
            tr.receiver.Apply( recv_pub);
            tr.amount_high = amount_high;
            tr.amount_low = amount_low;
            byte[] saltdata = new byte[raConstants.szKeyType];
            rnd.NextBytes(saltdata);
            BitConverter.GetBytes(dt.Ticks).CopyTo(saltdata, 0);
            tr.salt.Apply(saltdata);            
            byte[] signature = gen_signature(tr.GetBytes(64, 0),tr.sender, prv);
            if (signature !=null)
            {
                tr.signature.Apply(signature);
                ApiCmd c = new ApiCmd(ApiCommands.apiCommitTransaction);
                if (i_connect())
                {
                    //c.sz = 188;
                    sock.Send(c.GetBytes());
                    sock.Send(tr.GetBytes());
                    if (sendterminate)
                    {
                        c.cmd = ApiCommands.apiTerminatingBlock;
                        sock.Send(c.GetBytes());
                        byte[] recv = new byte[6];
                        sock.Receive(recv);
                    }
                }
            } else
            {
                i_errcode = -1;
                i_errmsg = "error of create signature ";
            }
        }
        /// <summary>
        /// returns set value of transactinons from node, set as parametr from ConfigManager.my_pubkey
        /// </summary>
        /// <param name="offset"> offset from zero value of transaction array </param>
        /// <param name="limit">value of get transactions</param>
        /// <returns>returns list of Transaction objects</returns>
        public List<Transaction> cmdGetTransactionsByKey(UInt64 offset, UInt16 limit)
        {
            List<Transaction> result = new List<Transaction>();
            if (ConfigManager.my_pubkey==null)
            {
                this.i_errcode = -1;
                this.i_errmsg = "sender public key not set";
                return result;
            }
            KeyType mykey = new KeyType(ConfigManager.my_pubkey);
            KeyType nodekey=this.cmdGetInfo(mykey);
            ApiCmd c = new ApiCmd(ApiCommands.apiGetTransactionsByKey);
            byte[] snd = new byte[c.sz];
            BitConverter.GetBytes(offset).CopyTo(snd, 64);
            BitConverter.GetBytes(limit).CopyTo(snd, 72);
            if (i_connect())
            {
                sock.Send(c.GetBytes());
                sock.Send(snd);
                c.cmd = ApiCommands.apiTerminatingBlock;
                sock.Send(c.GetBytes());
                byte[] r = new byte[6];
                do
                {
                    sock.Receive(r);
                    c.SetFromBytes(r);
                    if (c.cmd == ApiCommands.apiSendTransactionsByKey)
                    {
                        byte[] res = new byte[c.sz];
                        if (sock.Receive(res) == c.sz)
                        {
                            //todo set list
                            UInt64 off = BitConverter.ToUInt64(res, 0);
                            UInt16 lim = BitConverter.ToUInt16(res, 8);
                            for (int i=0;i<lim;i++)
                            {
                                byte[] tbt = new byte[64 + 32 + 32 + 4 + 8 + 16];
                                Array.Copy(res, 64 + (i * tbt.Length), tbt, 0, tbt.Length);
                                Transaction t = new Transaction(tbt);
                                result.Add(t);                                
                            }

                            sock.Receive(r);
                            c.SetFromBytes(r);
                            break;
                        }
                    }
                } while (c.cmd != ApiCommands.apiSendTransactionsByKey );
            }
            return result;
        }
        /// <summary>
        /// returns balance from set wallet 
        /// </summary>
        /// <param name="pub">id (pub key) of wallet </param>
        /// <returns>object type of Amount</returns>
        /// <remarks>if pub.empty(), public key always gets from ConfigMAnager</remarks>
        public Amount cmdGetBalance(KeyType pub)
        {
            Amount result = new Amount();
            
            KeyType mykey = new KeyType();
            if (!pub.empty())
                mykey.Apply(pub);
            else
                mykey.Apply(ConfigManager.my_pubkey);
            KeyType nodekey = this.cmdGetInfo(mykey);
            ApiCmd c = new ApiCmd(ApiCommands.apiGetBalance);
            byte[] snd = new byte[c.sz];
            if (i_connect())            {
                
                sock.Send(c.GetBytes());
                sock.Send(snd);
                c.cmd = ApiCommands.apiTerminatingBlock;
                sock.Send(c.GetBytes());
                byte[] r = new byte[6];
                do
                {
                    sock.Receive(r);
                    c.SetFromBytes(r);
                    if (c.cmd == ApiCommands.apiSendBalance)
                    {
                        byte[] recv = new byte[c.sz];
                        sock.Receive(recv);
                        result.hight = BitConverter.ToUInt32(recv, 0);
                        result.low = BitConverter.ToUInt64(recv, 4);
                        break;
                    }
                } while (c.cmd != ApiCommands.apiSendBalance);
            }

            return result;

        }
        /// <summary>
        /// returns balance for set wallet, wallet's key always get from ConfigManager
        /// </summary>
        /// <returns>Amount structure</returns>
        public Amount cmdGetBalance()
        {
            Amount result = new Amount();
            if (ConfigManager.my_pubkey == null)
            {
                this.i_errcode = -1;
                this.i_errmsg = "sender public key not set";
                return result;
            }
            KeyType mykey = new KeyType();
            mykey.Apply(ConfigManager.my_pubkey);
            KeyType nodekey = this.cmdGetInfo(mykey);
            ApiCmd c = new ApiCmd(ApiCommands.apiGetBalance);
            byte[] snd = new byte[c.sz];
            if (i_connect())
            {

                sock.Send(c.GetBytes());
                sock.Send(snd);
                c.cmd = ApiCommands.apiTerminatingBlock;
                sock.Send(c.GetBytes());
                byte[] r = new byte[6];
                do
                {
                    sock.Receive(r);
                    c.SetFromBytes(r);
                    if (c.cmd == ApiCommands.apiSendBalance)
                    {
                        byte[] recv = new byte[c.sz];
                        sock.Receive(recv);
                        result.hight = BitConverter.ToUInt32(recv, 0);
                        result.low = BitConverter.ToUInt64(recv, 4);
                        break;
                    }
                } while (c.cmd != ApiCommands.apiSendBalance);
            }

            return result;

        }
        /// <summary>
        /// Returns  Previous Hash, wrote in pointed block
        /// </summary>
        /// <param name="block"> block hash, from which returns Previous Hash</param>
        /// <returns></returns>
        public HashType cmdGetPrevHash(HashType block)
        {
            HashType result = new HashType();
            ApiCmd cmd = new ApiCmd(ApiCommands.apiGetPrevHash);
            if (i_connect())
            {
                sock.Send(cmd.GetBytes());
                sock.Send(block.data);
                cmd.cmd = ApiCommands.apiTerminatingBlock;
                sock.Send(cmd.GetBytes());
                byte[] r = new byte[6];
                cmd.SetFromBytes(r);
                while (cmd.cmd != ApiCommands.apiSendPrevHash)
                {
                    sock.Receive(r);
                    cmd.SetFromBytes(r);
                }
                byte[] resbt = new byte[cmd.sz];
                int res_sz = sock.Receive(resbt);
                if (res_sz == cmd.sz)
                {
                    result.Apply(resbt);
                }
            }
            return result;
        }
        /// <summary>
        /// Dispose method
        /// </summary>
        public void Dispose()
        {
            ((IDisposable)sock).Dispose();
        }
    }
}

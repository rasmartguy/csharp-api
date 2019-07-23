using System;
using System.Collections.Generic;
using System.Text;

namespace RAPIlib
{
    /// <summary>
    /// consts  (structer's length)
    /// </summary>
    public static class raConstants
    {
        public const UInt16 szTransaction = 64 + 32 + 32 + 4 + 8 + 16 + 32;
        public const UInt16 szHashType = 64;
        public const UInt16 szKeyType = 32;
    }
   
    /// <summary>
    /// class-parent for structs KeyType and HashType
    /// extended byte array
    /// </summary>
    public class Tarr : IEquatable<Tarr>
    {
        
        public byte[] data;        
        public int sz { get { return data.Length; } }
       
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            Tarr p_as = obj as Tarr;
            if (p_as == null)
                return false;
            else
                return Equals(p_as);
        }
        
        public bool Equals(Tarr other)
        {
            if (other == null) return false;
            if (other.data.Length != this.data.Length) return false;
            for (int i = 0; i < this.data.Length; i++)
                if (this.data[i] != other.data[i])
                    return false;
            return true;
        }

        /// <summary>
        /// defautl constructor empty byte array set length 
        /// </summary>
        /// <param name="init_sz">array's length</param>
        public Tarr(int init_sz) {  data = new byte[init_sz]; }
        /// <summary>
        /// create Tarr object from a byte array
        /// </summary>
        /// <param name="init_arr">исходный массив байт</param>
        public Tarr(byte[] init_arr) { data = new byte[init_arr.Length];init_arr.CopyTo(data, 0); }
        /// <summary>
        /// сreate Tarr object from byte array with length's control (child classes)
        /// </summary>
        /// <param name="init_arr">initial array </param>
        /// <param name="i_sz">set length</param>
        public Tarr(byte[] init_arr,int i_sz) { if (i_sz == init_arr.Length) { data = new byte[init_arr.Length]; init_arr.CopyTo(data, 0); } }
        /// <summary>
        /// overload operator
        /// </summary>
        /// <returns>represent byte array into string Х2</returns>
        public override string ToString()
        {
            string ret = "";
            for (int i = 0; i < sz; i++)
                ret += data[i].ToString("X2");
            return ret;
        }
        /// <summary>
        /// convert string Х2 into array(back method to ToString)
        /// </summary>
        /// <param name="str">string Х2</param>
        public void FromString(string str)
        {
            if (str.Length==(sz*2))
            {
                for (int i=0;i<sz;i++)
                    data[i]= Convert.ToByte(str.Substring(i * 2, 2), 16);
            }
        }
        /// <summary>
        /// check on empty (zero) массив
        /// </summary>
        /// <returns>returns true if all bytes equal to 0</returns>
        public bool empty()
        {
            bool result = true;
            for (int i = 0; i < sz; i++)
            {
                if (data[i] != 0)
                {
                    result = false;
                    break;
                }
            }
            return result;
        }
        /// <summary>
        /// copy content one Tarr into current
        /// </summary>
        /// <param name="t2"></param>
        public void Apply(Tarr t2)
        {
            if (this.sz>=t2.sz)
            {
                Array.Copy(t2.data, 0, this.data, 0, t2.data.Length);
            }
            
        }
        /// <summary>
        /// copy content of byte array into current
        /// </summary>
        /// <param name="t2"></param>
        public void Apply(byte[] t2)
        {
            if (this.sz >= t2.Length)
            {
                Array.Copy(t2, 0, this.data, 0, t2.Length);
            }

        }
    }
    /// <summary>
    /// class contains 64 byte array (private key, block hash, transaction signature
    /// </summary>
    public class HashType : Tarr
    {
        public HashType() : base(raConstants.szHashType) { }
        public HashType(byte[] bytes) : base(bytes,raConstants.szHashType) { }
    }
    /// <summary>
    /// class contains 32-byte array (public key)
    /// </summary>
    public class KeyType:Tarr
    {
        public KeyType() : base(raConstants.szKeyType) { }
        public KeyType(byte[] bytes) : base(bytes, raConstants.szKeyType) { }
    }
    /// <summary>
    /// Transaction class
    /// </summary>
    public class Transaction
    {
        public HashType signature;
        public KeyType sender;
        public KeyType receiver;
        public UInt32 amount_high;
        public UInt64 amount_low;
        public string currency;
        public KeyType salt;
        /// <summary>
        /// init empty Transaction
        /// </summary>
        public Transaction()
        {
            signature = new HashType();
            sender = new KeyType();
            receiver = new KeyType();
            currency = "";
            salt = new KeyType();
        }
        /// <summary>
        /// Init structure и fill field with value from byte array 
        /// </summary>
        /// <param name="bytes">array</param>
        public Transaction(byte[] bytes):this()
        {
            if (bytes.Length == raConstants.szTransaction)
            {
                Array.Copy(bytes, 0, signature.data, 0, 64);
                Array.Copy(bytes, 64, sender.data, 0, 32);
                Array.Copy(bytes, 96, receiver.data, 0, 32);
                amount_high = BitConverter.ToUInt32(bytes, 128);
                amount_low = BitConverter.ToUInt64(bytes, 132);
                currency = Encoding.ASCII.GetString(bytes, 140, 16);
                Array.Copy(bytes, 156, salt.data, 0, 32);
            }
        }
        /// <summary>
        /// return transaction or part as byte array (for sending)
        /// </summary>
        /// <param name="offset">offset in got array </param>
        /// <param name="length">length of got array</param>
        /// <remarks> if params==0 return full array (default)</remarks>
        /// <returns>byte array</returns>
        public byte[] GetBytes(int offset=0,int length=0)
        {
            byte[] result = new byte[raConstants.szTransaction];
            Array.Copy(signature.data, result, raConstants.szHashType);
            Array.Copy(sender.data, 0, result, raConstants.szHashType, raConstants.szKeyType);
            Array.Copy(receiver.data, 0, result, raConstants.szHashType + raConstants.szKeyType, raConstants.szKeyType);
            Array.Copy(BitConverter.GetBytes(amount_high), 0, result, raConstants.szHashType + raConstants.szKeyType + raConstants.szKeyType, sizeof(UInt32));
            Array.Copy(BitConverter.GetBytes(amount_low), 0, result, raConstants.szHashType + raConstants.szKeyType + raConstants.szKeyType+ sizeof(UInt32),sizeof(UInt64));
            byte[] b = Encoding.ASCII.GetBytes(currency);
            Array.Copy(b, 0, result, raConstants.szHashType + raConstants.szKeyType + raConstants.szKeyType + sizeof(UInt32)+ sizeof(UInt64),b.Length);
            Array.Copy(salt.data, 0, result, raConstants.szTransaction - raConstants.szKeyType, raConstants.szKeyType);
            if ((offset==0 && length==0)||(offset+length>raConstants.szTransaction))
                return result;
            else
            {
                int l = length == 0 ? raConstants.szTransaction - offset : length;
                byte[] result2 = new byte[l];
                Array.Copy(result, offset, result2, 0, l);
                return result2;
            }
        }
    }
    /// <summary>
    /// amount class of transaction
    /// </summary>
    public class Amount
    {
        /// <summary>
        /// "integral" part of amout
        /// </summary>
        public UInt32 hight;
        /// <summary>
        /// "fraction" par of amount
        /// </summary>
        public UInt64 low ;
        public Amount() { hight = 0;low = 0; }
        public Amount(UInt32 h,UInt64 l) { hight = h; low = l; }
        public Amount(Amount a) { hight = a.hight;low = a.low; }
        public static Amount operator +(Amount a1,Amount a2)
        {
            UInt32 h = a1.hight + a2.hight;
            UInt64 l = a1.low + a2.low;
            if (l>= 1000000000)
            {
                l -= 1000000000;
                h++;
            }
            return new Amount(h, l);
        }
        public static bool operator<(Amount a1, Amount a2)
        {
            if (a1.hight < a2.hight) return true;
            if (a1.hight == a2.hight) return a1.low < a2.low;
            return false;
        }
        public static bool operator >(Amount a1, Amount a2)
        {
            if (a1.hight > a2.hight) return true;
            if (a1.hight == a2.hight) return a1.low > a2.low;
            return false;
        }
        public static Amount operator-(Amount a1,Amount a2)
        {
            if (a1 < a2)
                throw new IndexOutOfRangeException("Negative amount detected by subtraction");
            UInt32 h = a1.hight - a2.hight;
            UInt64 l = a1.low;
            if (a2.low>l)
            {
                l += 1000000000;
                h--;
            }
            l -= a2.low;
            return new Amount(h, l);
        }
    }
}

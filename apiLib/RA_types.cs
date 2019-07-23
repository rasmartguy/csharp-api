using System;
using System.Collections.Generic;
using System.Text;

namespace RAPIlib
{
    /// <summary>
    /// перечень используемых констант (длин структур)
    /// </summary>
    public static class raConstants
    {
        public const UInt16 szTransaction = 64 + 32 + 32 + 4 + 8 + 16 + 32;
        public const UInt16 szHashType = 64;
        public const UInt16 szKeyType = 32;
    }
   
    /// <summary>
    /// класс-родитель для структур KeyType и HashType
    /// расширенный массив байт
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
        /// конструктор по умолчанию - пустой массив заданной длины
        /// </summary>
        /// <param name="init_sz">длина массива</param>
        public Tarr(int init_sz) {  data = new byte[init_sz]; }
        /// <summary>
        /// создает объект Tarr  из байтового массива
        /// </summary>
        /// <param name="init_arr">исходный массив байт</param>
        public Tarr(byte[] init_arr) { data = new byte[init_arr.Length];init_arr.CopyTo(data, 0); }
        /// <summary>
        /// создает объект Tarr  из байтового массива с контролем длины (для дочерних классовЗ
        /// </summary>
        /// <param name="init_arr">исходный массив</param>
        /// <param name="i_sz">заданная длина</param>
        public Tarr(byte[] init_arr,int i_sz) { if (i_sz == init_arr.Length) { data = new byte[init_arr.Length]; init_arr.CopyTo(data, 0); } }
        /// <summary>
        /// перегруженный оператор
        /// </summary>
        /// <returns>представление массива байт в виде строки Х2</returns>
        public override string ToString()
        {
            string ret = "";
            for (int i = 0; i < sz; i++)
                ret += data[i].ToString("X2");
            return ret;
        }
        /// <summary>
        /// конвертирует строку Х2 в массив (обратно методу ToString)
        /// </summary>
        /// <param name="str">строка Х2</param>
        public void FromString(string str)
        {
            if (str.Length==(sz*2))
            {
                for (int i=0;i<sz;i++)
                    data[i]= Convert.ToByte(str.Substring(i * 2, 2), 16);
            }
        }
        /// <summary>
        /// проверка на пустой (нулевой) массив
        /// </summary>
        /// <returns>возвр. true если все байты равны 0</returns>
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
        /// копирует содержимое одного Tarr в текущий
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
        /// копирует содержимое массива байт в текущий объект
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
    /// класс представляющий 64 байтовый массив (приватный ключ, хеш блока, сигнатура транзакции и т.д.
    /// </summary>
    public class HashType : Tarr
    {
        public HashType() : base(raConstants.szHashType) { }
        public HashType(byte[] bytes) : base(bytes,raConstants.szHashType) { }
    }
    /// <summary>
    /// класс представляющий 32-байтовый массив (публичный ключ)
    /// </summary>
    public class KeyType:Tarr
    {
        public KeyType() : base(raConstants.szKeyType) { }
        public KeyType(byte[] bytes) : base(bytes, raConstants.szKeyType) { }
    }
    /// <summary>
    /// класс представляет транзакцию
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
        /// инициализирует пустую транзакцию
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
        /// инициализирует структуру и заполняет ее значениями из массива байт
        /// </summary>
        /// <param name="bytes">исх массив</param>
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
        /// возвращает транзакцию или часть в виде массива байт (для передачи в сеть)
        /// </summary>
        /// <param name="offset">смещение в результирующем массиве</param>
        /// <param name="length">длина результирующего массива</param>
        /// <remarks> если оба параметра==0 возвращается массив целиком (default)</remarks>
        /// <returns>массив байт</returns>
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
    /// класс представление суммы транзакции
    /// </summary>
    public class Amount
    {
        /// <summary>
        /// "старшая" часть суммы
        /// </summary>
        public UInt32 hight;
        /// <summary>
        /// "младшая" часть суммы
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

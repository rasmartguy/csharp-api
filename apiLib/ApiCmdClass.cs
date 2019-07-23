using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAPIlib
{
    /// <summary>
    /// commanss API
    /// </summary>
    public enum ApiCommands : short
    {
        apiInvalid = -1,
        apiClose = apiInvalid,
        apiTerminatingBlock = 0,
        apiGetInfo,
        apiSendInfo,
        apiGetBalance,
        apiSendBalance,
        apiGetCounters,
        apiSendCounters,
        apiGetLastHash,
        apiSendLastHash,
        apiGetBlocks,
        apiSendBlocks,
        apiGetBlockSize,
        apiSendBlockSize,
        apiGetTransactions,
        apiSendTransactions,
        apiGetTransaction,
        apiSendTransaction,
        apiCommitTransaction,
        apiGetLastError,
        apiError,
        apiGetTransactionsByKey,
        apiSendTransactionsByKey,
        apiGetFee,
        apiSendFee,
        apiGetBinaryData,
        apiCommitBinaryData,
        apiGetBinaryPart,
        apiGetPrevHash,
        apiSendPrevHash,
        apiCount
    };
    /// <summary>
    /// gives command struct of API
    /// </summary>
    public class ApiCmd
    {
        private ApiCommands icmd;
        /// <summary>
        /// command
        /// </summary>
        public ApiCommands cmd
        {
            get { return icmd; }
            set
            {
                icmd = value;
                sz = send_size(icmd);
            }
        }
        /// <summary>
        /// length send\recv data
        /// </summary>
        public uint sz;
        /// <summary>
        /// constructor "empty" команды
        /// </summary>        
        public ApiCmd() { cmd = ApiCommands.apiInvalid; sz = 0; }
        /// <summary>
        /// constructor with code of command (sets lenght of data by cmd)
        /// </summary>
        /// <param name="ncmd">command's code</param>
        public ApiCmd(ApiCommands ncmd)
        {
            cmd = ncmd;
            sz = send_size(ncmd);
            
        
        }
        private uint send_size(ApiCommands cmd)
        {
            switch (cmd)
            {
                case ApiCommands.apiGetInfo:
                    return raConstants.szKeyType;                     
                case ApiCommands.apiGetBlocks:
                    return 10;
                case ApiCommands.apiGetBalance:
                    return 16;
                case ApiCommands.apiGetBlockSize:
                    return raConstants.szHashType; 
                case ApiCommands.apiGetTransactions:
                    return 64 + sizeof(UInt64) + sizeof(UInt16);
                case ApiCommands.apiGetTransaction:
                    return 64 + 64; 
                case ApiCommands.apiCommitTransaction:
                    return 64+32 + 32 + sizeof(UInt32) + sizeof(UInt64) + 16 + 0x20;
                case ApiCommands.apiGetTransactionsByKey:
                    return 64 + sizeof(UInt64) + sizeof(UInt16);
                case ApiCommands.apiGetBinaryData:
                    return 66;
                case ApiCommands.apiGetBinaryPart:
                    return 64 + sizeof(ushort) +(sizeof(uint) * 2);
                case ApiCommands.apiGetPrevHash:
                    return raConstants.szHashType;
                default:
                   return 0;
                    
            }
        }
        /// <summary>
        ///  convert buffer into binary array for sending into socket
        /// </summary>
        /// <returns>array for send</returns>
        public byte[] GetBytes()
        {
            byte[] result = new byte[6];
            BitConverter.GetBytes((short)cmd).CopyTo(result, 0);
            BitConverter.GetBytes(sz).CopyTo(result, 2);
            return result;
        }
        /// <summary>
        /// convert binary array from socket into command 
        /// </summary>
        /// <param name="buf">got array</param>
        public void SetFromBytes(byte[] buf)
        {
            if (buf.Length < 6) return;
            cmd = (ApiCommands)BitConverter.ToInt16(buf, 0);
            sz = BitConverter.ToUInt32(buf, 2);
        }
    }
}

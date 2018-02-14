using UnityEngine;
using System;
using System.Collections;
using System.IO.Ports;
using System.IO;
using System.Threading;


/**
 * @mainpage VAQSO VRライブラリ
 * <p>VAQSO VRライブラリメインクラス</p>
 */

namespace VAQSOVR
{
    public class Scent
    {
        private static SerialPort serialPort;
        private static Thread readThread;

        private static bool _continue = false;
        private static bool isWrite = false;
        private static bool isConnected = false;

        private static string errorMsg = "";

        public enum RESPONSE_VR_UNIT
        {
            RESULTS_NONE,
            RESULTS_OK,
            RESULTS_NG,
            RESULTS_RECEIVE,
        }

        private static RESPONSE_VR_UNIT isResponse;

        /// <summary> デバイス状態 </summary>
        public enum State
        {
            /// <summary> None </summary>
            None = 0x00,
            /// <summary> 接続中 </summary>
            Connectting = 0x01,
            /// <summary> 接続 </summary>
            Connected = 0x02,
            /// <summary> 切断 </summary>
            Disconnected = 0x03,
        }

        /// <summary> デバイスステータス更新イベント </summary>
        /// <param name="state">デバイスステータス</param>
        public delegate void onStateChanged(State state);

        /// <summary> デバイスステータス更新イベントデリゲート変数 </summary>
        public static onStateChanged StateChanged;


        /// <summary> 匂いスロット </summary>
        public enum Slot
        {
            A = 0x41,
            B = 0x42,
            C = 0x43,
        }


        public static bool Open(string portName)
        {
            if (serialPort == null)
            {
                //serialPort = new SerialPort();
				serialPort = new SerialPort("\\\\.\\"+portName, 9600, Parity.None, 8, StopBits.One);
            }
            
            //serialPort.PortName = portName;
			//serialPort.BaudRate = 9600;
			//serialPort.Parity = Parity.None;
			//serialPort.DataBits = 8;
			//serialPort.StopBits = StopBits.One;
			//serialPort.Handshake = Handshake.None;

            serialPort.ReadTimeout = 1000;
            serialPort.WriteTimeout = 1000;

            try
            {
                serialPort.Open();
                _continue = true;

                if (readThread == null)
                {
                    readThread = new Thread(Read);
                    readThread.Start();
                }

                SetConnect();
            }
            catch (UnauthorizedAccessException)
            {
                Debug.LogError("Unauthorized Access Exception");

                serialPort.Dispose();
                serialPort = null;
                return false;
            }
            catch (IOException)
            {
                StateChanged(State.None);
                errorMsg = string.Format("IOException: The port '{0}' does not exist.", portName);                

                serialPort.Dispose();
                serialPort = null;
                return false;
            }

            return true;
        }

        public static void SetConnect()
        {
            if (!isConnected)
            {
                //接続する
                Connect();
            }
        }

        public static void SetDisconnect()
        {
            //接続中
            if (isConnected)
            {
                //切断する
                Disconnect();
            }
        }

        public static void Shoot(Slot slot, int adjust)
        {
            //接続中
            if (isConnected)
            {
                //風量：adjust
                Shoot_(slot, adjust);
            }
        }

        public static void Close()
        {
            SetDisconnect();

            _continue = false;

            if (readThread != null && readThread.IsAlive)
            {
                readThread.Abort();
                readThread.Join();
                readThread = null;
            }

            if (serialPort != null && serialPort.IsOpen)
            {
                serialPort.Close();
                serialPort.Dispose();
                serialPort = null;
            }
        }

        public static RESPONSE_VR_UNIT GetResponse()
        {
            return isResponse;
        }

        //接続する
        static void Connect()
        {
            byte[] writebyte = new byte[6];

            writebyte[0] = 0x02;
            writebyte[1] = 0x63;
            writebyte[2] = 0x03;
            writebyte[3] = 0x68;
            writebyte[4] = 0x0D;
            writebyte[5] = 0x0A;

            Write(writebyte, 0, 6);
            isConnected = true;

            StateChanged(State.Connected);
        }

        //切断する
        static void Disconnect()
        {
            byte[] writebyte = new byte[6];

            writebyte[0] = 0x02;
            writebyte[1] = 0x64;
            writebyte[2] = 0x03;
            writebyte[3] = 0x69;
            writebyte[4] = 0x0D;
            writebyte[5] = 0x0A;

            Write(writebyte, 0, 6);

            Debug.Log(GetCommand(writebyte[1]));

            StateChanged(State.Disconnected);

            isConnected = false;
        }

        /// <summary> 匂いイベントを発生させる </summary>
    	/// <param name="slot">  匂いスロット</param>
    	/// <param name="adjust">匂いの値 (0-100)</param>
		static void Shoot_(Slot slot, int adjust)
        {
            byte[] writebyte = new byte[10];

            //STX
            writebyte[0] = 0x02;
            //Shoot
            writebyte[1] = 0x73;
            //Slot
            writebyte[2] = (byte)slot;

            //Adjust
            char p1 = char.Parse((adjust / 100).ToString());
            char p2 = char.Parse(((adjust % 100) / 10).ToString());
            char p3 = char.Parse((adjust % 10).ToString());
            writebyte[3] = (byte)p1;
            writebyte[4] = (byte)p2;
            writebyte[5] = (byte)p3;

            //ETX
            writebyte[6] = 0x03;

            //SUM作成
            int sum = 0;
            for (int i = 0; i < 7; i++)
            {
                sum += writebyte[i];
            }
            writebyte[7] = (byte)(sum & 0xFF);            

            //CR
            writebyte[8] = 0x0D;
            //LF
            writebyte[9] = 0x0A;

            Write(writebyte, 0, 9);
        }

        //Portからの入力を読み取る
        public static void Read()
        {
            byte[] readByte = new byte[8];
            int count = 0;

            while (_continue && serialPort != null && serialPort.IsOpen)
            {
                if (isWrite)
                {
                    try
                    {
                        int value = serialPort.ReadByte();
                        //int value = serialPort.Read(readByte, 0, 1);
                        if (value > 0)
                        {
                            readByte[count] = (byte)value;
                            count++;

                            isResponse = RESPONSE_VR_UNIT.RESULTS_RECEIVE;
                        }

                        //if (count == 8)
                        if (0 < count)
                        {
                            //Debug.Log(GetCommand(readByte[1]) + " : " + GetResult(readByte));

                            // 現状返信の中身は無視をして８byte返ってきたらOKにする
                            //if (GetResult(readByte) == "OK")
                            {
                                Debug.Log("Read");

                                isResponse = RESPONSE_VR_UNIT.RESULTS_OK;
                            }
                            //else
                            //{
                                //isResponse = RESPONSE_VR_UNIT.RESULTS_NG;
                            //}

                            for (int i = 0; i < readByte.Length; i++)
                            {
                                readByte[i] = 0;
                            }

                            isWrite = false;
                            count = 0;
                        }
                    }
                    catch (TimeoutException)
                    {
                    }
                }
                else
                {
                    isResponse = RESPONSE_VR_UNIT.RESULTS_NONE;
                }
            }
        }


        //Portに書き出す。
        static void Write(byte[] data, int offset, int count)
        {
            try
            {
                serialPort.Write(data, offset, count);

                isWrite = true;
            }
            catch (TimeoutException)
            {
                Debug.LogError("Timeout Exception");
            }
        }

        //コマンド名を取得
        static string GetCommand(byte data)
        {
            string command = "";

            switch (data)
            {
                case 0x63:
                    command = "Connect";                    
                    break;
                case 0x64:
                    command = "Disconnect";                    
                    break;
                case 0x73:
                    command = "Shoot";
                    break;
            }

            return command;
        }

        //結果を取得
        static string GetResult(byte[] data)
        {
            string result = "";

            if (data[2] == 0x4f && data[3] == 0x4b)
            {
                result = "OK";
            }
            else if (data[2] == 0x4e && data[3] == 0x47)
            {
                result = "NG";
            }

            return result;  
        }

        static string[] GetPortNames()
        {
            string[] portNames;
            portNames = System.IO.Ports.SerialPort.GetPortNames();

            if (portNames.Length > 0)
                return portNames;
            else
                return null;
        }

        public static bool IsOpen()
        {
            return serialPort.IsOpen;
        }

        public static string[] GetPort()
        {
            return GetPortNames();
        }

        public static void SetErrorMsg()
        {
            errorMsg = "";
        }

        public static string GetErrorMsg()
        {
            return errorMsg;
        }
    }
}

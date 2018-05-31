

//#define VAQSO_USE_READ_THREAD	//シリアルポートからの読み込みスレッドを使用するか（現状、デバイスからの受信を使用していない）

using UnityEngine;
using System;
using System.IO.Ports;
using System.IO;
using System.Threading;

//VAQSO
namespace VAQSOVR
{	
	//香りデバイス
	public static class Scent
	{
		//デバイス状態
		public enum STATE
		{
			NONE,			//初期状態
			CONNECTING,		//接続中
			CONNECTED,		//接続された
			CONNECT_FAILED,	//接続失敗
			DISCONNECTED,	//切断された
		}

		public	const	int			SlotNum		= 5;				//匂いスロット数

		//接続コマンド
		private static readonly byte[] connectCommand		= new byte[6]{0x02,0x63,0x03,0x68,0x0D,0x0A};
		//切断コマンド
		private static readonly byte[] disconnectCommand	= new byte[6]{0x02,0x64,0x03,0x69,0x0D,0x0A};
		
		private	static	SerialPort	serialPort;                     //シリアルポート

//デバイスから情報を受信する場合
#if VAQSO_USE_READ_THREAD
		private	static	Thread		readThread;						//デバイスからの情報を読み取るスレッド
		private static	bool		continueReadThread = false;		//デバイスから情報を読み取り続けるかフラグ
#endif

		//現在のデバイス状態
		public	static	STATE		NowState		{ get; private set; }	

		//デバイスの接続状態	
		public	static	bool		IsConnected		{ get{ return NowState == STATE.CONNECTED; } }						
		
		//匂いの値
		static float[] values = new float[SlotNum]{0,0,0,0,0};
		public static float[] Nowvalues{get{return values; } }

		//デバイスステータス更新イベント
		public delegate void onStateChanged(STATE state);

		//デバイスステータス更新イベントデリゲート変数
		static onStateChanged StateChanged;

		//接続
		public static bool Connect(string portName)
		{
			//接続中、接続済みなら処理しない
			if (NowState == STATE.CONNECTING || NowState == STATE.CONNECTED){
				return false;
			}

			//接続中
			NowState = STATE.CONNECTING;
			StateChanged( STATE.CONNECTING );

			if(serialPort == null) {
				serialPort = new SerialPort(portName,9600,Parity.None,8,StopBits.One);
			}

			serialPort.ReadTimeout = 1000;
			serialPort.WriteTimeout = 1000;

			try {
				//シリアルポートを開く
				serialPort.Open();


//デバイスから情報を受信する場合
#if VAQSO_USE_READ_THREAD
				
				//受信スレッド
				if (readThread == null)
				{
					continueReadThread = true;
				    readThread = new Thread(ReadPort);
				    readThread.Start();
				}
				
#endif

				//接続命令をデバイスに送る
				Connect_Post();

			} catch(UnauthorizedAccessException) {
				Debug.LogWarning("Unauthorized Access Exception");

				serialPort.Dispose();
				serialPort = null;
				return false;
			} catch(IOException) {
				StateChanged( STATE.CONNECT_FAILED );

				Debug.LogWarning("IOException: The port '"+portName+"' does not exist.");

				serialPort.Dispose();
				serialPort = null;
				return false;
			} catch(Exception) {
				Debug.LogWarning("Exception");

				serialPort.Dispose();
				serialPort = null;
				return false;
			}

			return true;
		}

		//匂いの数値設定
		public static void SetValue(int slot,int value)
		{
			//接続中
			if( NowState == STATE.CONNECTED ) {
				//風量：adjust
				SetValue_Post(slot,value);
			}
		}

		//閉じる
		public static void Disconnect()
		{
			if( NowState != STATE.CONNECTED ) return;

			//全スロットのファン停止
			for(int i = 0;i < SlotNum;i++) {
				SetValue_Post(i,0);
			}


			//切断命令をデバイスに送信
			Disconnect_Post();

//デバイスから情報を受信していた場合
#if VAQSO_USE_READ_THREAD
			//スレッドを始末
			continueReadThread = false;
			if( readThread != null && readThread.IsAlive ){
				readThread.Join();
			}
			readThread = null;
#endif
			
			//シリアルポートを始末
			if( serialPort != null && serialPort.IsOpen ){
				serialPort.Close();
				serialPort.Dispose();
				serialPort = null;
			}
		}

		//状態が変更されたときに呼び出される関数を追加
		public static void AddOnStateChangeEvent(onStateChanged addFunc)
		{
			StateChanged += addFunc;
		}

		//状態が変更されたときに呼び出される関数を追加
		public static void RemoveOnStateChangeEvent(onStateChanged removeFunc)
		{
			StateChanged -= removeFunc;
		}


		#region Serial communication

		//接続命令をデバイスに送信
		static void Connect_Post()
		{
			WritePort(connectCommand,0,6);

			NowState = STATE.CONNECTED;
			StateChanged(STATE.CONNECTED);
		}

		//切断命令をデバイスに送信
		static void Disconnect_Post()
		{
			WritePort(disconnectCommand,0,6);

			NowState = STATE.DISCONNECTED;
			StateChanged(STATE.DISCONNECTED);
		}

		//匂いの数値変更をデバイスに送信
		static void SetValue_Post(int slot,int value)
		{
			//同じ値を再送信しない
			if( value == values[slot] ) return;

			values[slot] = value;

			string	data = ("fan " + slot.ToString() + " " + value.ToString() + "\r\n");

			WritePort(data);
		}

		//シリアルポートに書き出す
		static void WritePort(byte[] data,int offset,int count)
		{
			try {
				serialPort.Write(data,offset,count);
			} catch(TimeoutException) {
				Debug.LogWarning("VAQSO書き込みタイムアウト");
			}
		}

		//シリアルポートに書き出す
		static void WritePort(string msg)
		{
			try {
				serialPort.Write(msg);
			} catch(TimeoutException) {
				Debug.LogWarning("VAQSO serial port write timeout");
			}
		}

//デバイスから情報を受信する場合
#if VAQSO_USE_READ_THREAD

		//シリアルポートから送られてくる情報を読むスレッド関数
		public static void ReadPort()
        {
            while ( continueReadThread && serialPort != null && serialPort.IsOpen)
            {
				try
				{
					string message = serialPort.ReadLine();
					Debug.Log("SerialPort Read:"+message);
				}
				catch (Exception)
				{
				}
            }
        }

#endif

	}

	#endregion
}

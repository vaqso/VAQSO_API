using UnityEngine;
using VAQSO;
using System;
using System.IO;

//VAQSOの制御マネージャー
public class VAQSOManager : MonoBehaviour
{

	//現在設定している香りの値
	private int[] values = new int[5]{0,0,0,0,0};
	public int[] Values
	{
		get{return values;}
	}

	//状態変更を受け取るデリゲート
	private ScentDevice.onStateChanged    stateChangedDelegate;

	//VAQSOのポート名を記述子たファイル(ポート名は環境毎に異なるので、EXEを作成した後にも変更できるようにファイルから読み込みます)
	public string portFileName = "VAQSO.txt";
	//使用するポート名
	string portName = "";

	void Awake()
	{
		//VAQSOポート名が記述されたファイルを読む
		string path = Application.dataPath+"/../"+portFileName;
		try{
			StreamReader reader = new StreamReader(path);
			portName = reader.ReadLine();
			reader.Close();
		}catch(Exception ex){
			Debug.LogError("VAQSOポートテキストが読めなかった："+path);
		}

		//VAQSOに接続
		ConnectVAQSO();
	}

	//VAQSOの状態が変わった
	void OnChangeState(ScentDevice.STATE state)
	{
		switch(state) {
			// 接続中
			case ScentDevice.STATE.CONNECTING:
				Debug.Log( "VAQSO 接続中" );
			break;

			// 接続完了
			case ScentDevice.STATE.CONNECTED:
				Debug.Log( "VAQSO 接続完了" );
			break;

			// 接続失敗
			case ScentDevice.STATE.CONNECT_FAILED:
				Debug.Log( "VAQSO 接続失敗" );
			break;

			// 切断
			case ScentDevice.STATE.DISCONNECTED:
				Debug.Log( "VAQSO 切断" );
			break;
		}

	}

	//全ての香りを止める
	public void StopAllScent()
	{
		for(int i = 0;i < values.Length;i++) {
			Values[i] = 0;
			ScentDevice.SetValue( i, 0 );
		}
	}

	//香り発射：スロット指定
	public void SetValue(int slot,int value)
	{
		//スロットチェック
		if( slot < 0 || slot > Values.Length ){
			Debug.LogError("VAQSO スロット外:"+slot);
			return;
		}

		Values[(int)slot] = value;
		ScentDevice.SetValue(slot,value);
	}
	public void SetValue(int slot,float value)
	{
		//スロットチェック
		if( slot < 0 || slot > Values.Length ){
			Debug.LogError("VAQSO スロット外:"+slot);
			return;
		}

		Values[(int)slot] = Mathf.RoundToInt(value);
		ScentDevice.SetValue(slot,Mathf.RoundToInt(value));
	}

	//VAQSOへの接続を試みる
	public void ConnectVAQSO()
	{
		//状態変更デリゲートを追加
		ScentDevice.AddOnStateChangeEvent( OnChangeState );
		//VAQSO接続
		ScentDevice.Connect( portName );
	}

	//VAQSOの接続を切る
	public void DisconnectVAQSO()
	{
		//VAQSO切断
		ScentDevice.Disconnect();
		//状態変更デリゲートを取り外し
		ScentDevice.RemoveOnStateChangeEvent( OnChangeState );
	}

	//破棄された時
	void OnDestroy()
	{
		DisconnectVAQSO();
	}	
}

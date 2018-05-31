using UnityEngine;
using VAQSOVR;
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
	private Scent.onStateChanged    stateChanged;

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
		Connect();
	}

	//VAQSOの状態が変わった
	void OnStateChanged(Scent.STATE state)
	{
		switch(state) {
			// 接続中
		case VAQSOVR.Scent.STATE.CONNECTING:
				Debug.Log( "VAQSO 接続中" );
			break;

			// 接続完了
		case VAQSOVR.Scent.STATE.CONNECTED:
				Debug.Log( "VAQSO 接続完了" );
			break;

			// 接続失敗
		case VAQSOVR.Scent.STATE.CONNECT_FAILED:
				Debug.Log( "VAQSO 接続失敗" );
			break;

			// 切断
		case VAQSOVR.Scent.STATE.DISCONNECTED:
				Debug.Log( "VAQSO 切断" );
			break;
		}

	}

	//全ての香りを止める
	public void StopAllScent()
	{
		for(int i = 0;i < values.Length;i++) {
			Values[i] = 0;
			VAQSOVR.Scent.SetValue( i, 0 );
		}
	}

	//香り発射：スロット指定
	public void Scent(int slot,int value)
	{
		//スロットチェック
		if( slot < 0 || slot > Values.Length ){
			Debug.LogError("VAQSO スロット外:"+slot);
			return;
		}

		Values[(int)slot] = value;
		VAQSOVR.Scent.SetValue(slot,value);
	}
	public void Scent(int slot,float value)
	{
		//スロットチェック
		if( slot < 0 || slot > Values.Length ){
			Debug.LogError("VAQSO スロット外:"+slot);
			return;
		}

		Values[(int)slot] = Mathf.RoundToInt(value);
		VAQSOVR.Scent.SetValue(slot,Mathf.RoundToInt(value));
	}

	//VAQSOへの接続を試みる
	public void Connect()
	{
		//状態変更デリゲートを追加
		VAQSOVR.Scent.AddOnStateChangeEvent( OnStateChanged );
		//VAQSO接続
		VAQSOVR.Scent.Connect( portName );
	}

	//VAQSOの接続を切る
	public void Disconnect()
	{
		//VAQSO切断
		VAQSOVR.Scent.Disconnect();
		//状態変更デリゲートを取り外し
		VAQSOVR.Scent.RemoveOnStateChangeEvent( OnStateChanged );
	}

	//破棄された時
	void OnDestroy()
	{
		Disconnect();
	}	
}

using UnityEngine;
using System.Collections;
using VAQSOVR;

public class VAQSOManager : MonoBehaviour
{
    public string PortName = "COM3";

	bool Connectting = false;
	bool Connected = false;
	public int A = 0;
	public int B = 0;
	public int C = 0;
	
	private Scent.onStateChanged stateChanged;
	private Scent.State state;
	
	void StateStart()
	{		
		// 接続ステータス変更イベント
		stateChanged = (Scent.State state) =>
		{
            Scent.SetErrorMsg();

			switch (state)
			{
			// None
			case Scent.State.None:
				Connectting = false;
				Connected = false;
				break;
				
			// 接続中
			case Scent.State.Connectting:
				//Debug.Log("VAQSO:接続中");
				Connectting = true;
                Connected = false;
                break;
				
            // 接続
			case Scent.State.Connected:
                    //Debug.Log("VAQSO:接続確立");
                    Connectting = false;
				Connected = true;
				break;
				
            // 切断
			case Scent.State.Disconnected:
                    //Debug.Log("VAQSO:切断");
                    Connectting = false;
                    Connected = false;
				break;
			}
		};
        Scent.StateChanged += stateChanged;
	}

	void Awake()
	{
		StateStart();
	}

    //受信した信号(message)に対する処理
    void OnDataReceived(string message)
	{
		var data = message.Split(
			new string[]{"\t"}, System.StringSplitOptions.None);
		if (data.Length < 2) return;
		
		try {
			Debug.Log(message);
		} catch (System.Exception e) {
			Debug.LogWarning(e.Message);
		}
	}
	
	
	void Stop()
	{
		Shoot(0, 0, 0);
	}
	
	void Shoot(int a, int b, int c)
	{
		A = a;
		B = b;
		C = c;
        Scent.Shoot(Scent.Slot.A, a);
        Scent.Shoot(Scent.Slot.B, b);
        Scent.Shoot(Scent.Slot.C, c);
	}
	
	void ShootA(int value)
	{
		A = value;
        Scent.Shoot(Scent.Slot.A, value);
    }
	void ShootB(int value)
	{
		B = value;
        Scent.Shoot(Scent.Slot.B, value);
    }
	void ShootC(int value)
	{
		C = value;
        Scent.Shoot(Scent.Slot.C, value);
    }
	
	void Update()
	{
		if (Input.GetKeyDown("0"))
			Stop();
		if (Input.GetKeyDown("1"))
			ShootA(100);
		if (Input.GetKeyDown("2"))
			ShootB(100);
		if (Input.GetKeyDown("3"))
			ShootC(100);
	}
	
	void OnDestroy()
	{
        if (Connected)
            Scent.Close();

        Scent.StateChanged -= stateChanged;
	}
	
	void OnGUI()
	{
		string state = "";        

        int button = 12;

        GUILayout.TextArea("  VAQSO VR - MONITOR  ");
		Rect screenRect = new Rect(10, 25, 256, 25 * (1 + button));
		GUILayout.BeginArea(screenRect);

        // FPS
        float fps = 1f / Time.deltaTime;
        GUILayout.TextField("FPS     : " + fps);

        {
            state = Connected ? "OK" : "NG";
            if (Connectting)
                state = "接続中";

            if (Scent.GetErrorMsg() != "")
                GUILayout.TextField("Message     : " + Scent.GetErrorMsg());

            PortName  = GUILayout.TextField(PortName);

            GUILayout.TextField("Status      : " + state);

            {
                if (!Connected)
                {
                    if (Connectting)
                    {
                        if (GUILayout.Button("Connecting...")) {}
                    }
                    else
                    {
                        if (GUILayout.Button("Connect"))
                        {
                            Connectting = true;
                            Scent.Open(PortName);
                        }
                    }
                }
                else if (!Connectting)
                {
                    if (GUILayout.Button("DisConnect"))
                    {
                        Scent.Close();
                    }
                }

                if (GUILayout.Button("A:" + A))
                {
                    Shoot(100, 0, 0);
                }
                if (GUILayout.Button("B:" + B))
                {
                    Shoot(0, 100, 0);
                }
                if (GUILayout.Button("C:" + C))
                {
                    Shoot(0, 0, 100);
                }
                if (GUILayout.Button("STOP"))
                {
                    Stop();
                }
            }
		}
		
		GUILayout.EndArea();
    }
}

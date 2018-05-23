using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VAQSO_DEMO:MonoBehaviour
{
	[SerializeField]
	VAQSOManager manager;
	[SerializeField]
	Slider[] sliders = new Slider[5];
	[SerializeField]
	Button[] buttons = new Button[5];
	[SerializeField]
	Button	stopButton;

	private void Awake()
	{
		//各UIに処理を割り当て
		for(int i = 0;i < sliders.Length;i++)
		{
			int slot = i;
			//スライダー
			sliders[i].onValueChanged.AddListener( (value)=>{ manager.SetValue(slot,value);} );
			//ボタン
			buttons[i].onClick.AddListener( ()=>{ SlotButton(slot); } );
		}

		stopButton.onClick.AddListener( StopAll );
	}

	//スロットボタンが押された
	private void SlotButton(int slot)
	{
		for(int i = 0;i < sliders.Length;i++) {
			sliders[i].value = (i==slot)? 100.0f:0.0f;
		}
	}

	//ストップボタンが押された
	private void StopAll()
	{
		for(int i = 0;i < sliders.Length;i++) {
			sliders[i].value = 0.0f;
		}
	}
}

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace JX_Plugin
{
	public class RankPanel : MonoBehaviour
	{
		public GameObject RankObj,contentContainer;
		public Button cancelBtn;
		public List<string> RankName = new List<string>();

		// Use this for initialization
		void Start()
		{
			cancelBtn.onClick.AddListener(OnCancelClick);
            if (RankName.Count > 0)
            {
				foreach(string s in RankName)
                {
					AddRank(s);
                }
            }
		}

		// Update is called once per frame
		public void OnCancelClick()
        {
			Destroy(this.gameObject);
        }

		void AddRank(string name)
        {
			GameObject obj = GameObject.Instantiate(RankObj);
			obj.transform.SetParent(contentContainer.transform);
			obj.GetComponent<Text>().text = name;
        }
	}
}


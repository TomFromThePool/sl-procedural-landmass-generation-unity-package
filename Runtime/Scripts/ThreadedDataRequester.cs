using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Threading;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ThreadedDataRequester : MonoBehaviour {

	#if UNITY_EDITOR
	static ThreadedDataRequester()
	{
		EditorApplication.update += ProcessQueueStatic;
	}
	#endif
	
	static ThreadedDataRequester _instance;

	static ThreadedDataRequester Instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = FindObjectOfType<ThreadedDataRequester>();
			}

			return _instance;
		}
	}
	
	Queue<ThreadInfo> dataQueue = new Queue<ThreadInfo>();
	
	public static void RequestData(Func<object> generateData, Action<object> callback)
	{
		var i = Instance;
		ThreadStart threadStart = delegate {
			i.DataThread (generateData, callback);
		};

		new Thread (threadStart).Start ();
	}

	void DataThread(Func<object> generateData, Action<object> callback) {
		object data = generateData ();
		lock (dataQueue) {
			dataQueue.Enqueue (new ThreadInfo (callback, data));
		}
	}
	
	void Update() {
		if (Application.isPlaying)
		{
			ProcessQueue();
		}
	}

	internal void ProcessQueue()
	{
		if (dataQueue.Count > 0)
		{
			for (int i = 0; i < dataQueue.Count; i++)
			{
				ThreadInfo threadInfo = dataQueue.Dequeue();
				threadInfo.callback(threadInfo.parameter);
			}
		}
	}

	static void ProcessQueueStatic()
	{
		Instance.ProcessQueue();
	}

	struct ThreadInfo {
		public readonly Action<object> callback;
		public readonly object parameter;

		public ThreadInfo (Action<object> callback, object parameter)
		{
			this.callback = callback;
			this.parameter = parameter;
		}

	}
}

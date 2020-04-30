using Common.Generic;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Helpers
{
	public class ThreadEx
	{
		private class TimerExecuter
		{
			private bool _isworking = false;

			private Semaphore _semaphore = new Semaphore(0, 1);

			private TimerInfo _timerInfo;

			private Thread _loopThread = null;

			private Action<TimerExecuter> _completed;

			private bool _rerun = false;

			private object _syncObj = new object();

			public bool IsRuning
			{
				get;
				private set;
			}

			public long CompleteTime
			{
				get;
				private set;
			}

			public Thread Thread => _loopThread;

			public TimerExecuter(Action<TimerExecuter> completed)
			{
				_completed = completed;
			}

			public void Run(TimerInfo info)
			{
				if (!_isworking)
				{
					throw new Exception("status error");
				}
				if (IsRuning)
				{
					throw new Exception("status error");
				}
				_timerInfo = info;
				_timerInfo.TimerThread = this;
				if (!_DEBUG)
				{
					_semaphore.Release(1);
				}
			}

			public void ReRun(TimerInfo info)
			{
				if (!_isworking)
				{
					throw new Exception("status error");
				}
				if (IsRuning)
				{
					throw new Exception("status error");
				}
				_rerun = true;
				_timerInfo = info;
				_timerInfo.TimerThread = this;
			}

			public void Start()
			{
				if (!_isworking)
				{
					_isworking = true;
					_loopThread = ThreadCall(Loop);
				}
			}

			public void Stop()
			{
				if (_isworking)
				{
					_isworking = false;
					ThreadStop(_loopThread);
				}
			}

			public void Reset()
			{
				if (_isworking)
				{
					_loopThread.Abort();
					_loopThread.Join(10);
					_semaphore.Close();
					_semaphore = new Semaphore(0, 1);
					if (_timerInfo != null && _timerInfo.TimerThread != null)
					{
						_timerInfo.TimerThread = null;
					}
					_timerInfo = null;
					IsRuning = false;
					_loopThread = ThreadCall(Loop);
					ThreadReset(this);
				}
			}

			private void Loop()
			{
				Loop(null);
			}

			private void Loop(object obj)
			{
				while (_isworking)
				{
					if (_DEBUG)
					{
						if (_timerInfo == null)
						{
							Thread.Sleep(5);
							continue;
						}
					}
					else if (!_rerun)
					{
						_semaphore.WaitOne();
					}
					_rerun = false;
					IsRuning = true;
					Call();
					IsRuning = false;
					OnCompleted();
					CompleteTime = TickCount;
				}
			}

			private void Call()
			{
				TimerInfo timerInfo = _timerInfo;
				object syncObject = default(object);
				try
				{
					bool lockTaken = false;
					try
					{
						Monitor.Enter(syncObject = timerInfo.SyncObject, ref lockTaken);
						timerInfo.IsRuning = true;
						timerInfo.IsReady = false;
					}
					finally
					{
						if (lockTaken)
						{
							Monitor.Exit(syncObject);
						}
					}
					timerInfo.LastBeginTime = TickCount;
					timerInfo.Callback();
					timerInfo.LastEndTime = TickCount;
					_timerInfo = null;
				}
				catch (Exception ex)
				{
					if (!(timerInfo?.IsFree ?? true))
					{
						timerInfo.IsError = true;
						timerInfo.Error = ex;
						timerInfo.LastEndTime = TickCount;
						OnError(ex);
					}
					_timerInfo = null;
				}
				finally
				{
					if (!(timerInfo?.IsFree ?? true))
					{
						bool lockTaken2 = false;
						try
						{
							Monitor.Enter(syncObject = timerInfo.SyncObject, ref lockTaken2);
							timerInfo.IsRuning = false;
							timerInfo.TimerThread = null;
						}
						finally
						{
							if (lockTaken2)
							{
								Monitor.Exit(syncObject);
							}
						}
					}
				}
			}

			private void OnCompleted()
			{
				_completed(this);
			}

			private void OnError(Exception e)
			{
				if (_timerInfo.ErrorHandle != null)
				{
					_timerInfo.ErrorHandle.BeginInvoke(e, null, null);
				}
				FreeTimer(_timerInfo.Key);
			}
		}

		private class TimerInfo
		{
			public Guid Key;

			public long LastBeginTime;

			public long LastEndTime;

			public string Name;

			public Action Callback;

			public int Interval;

			public bool IsReady;

			public bool IsRuning;

			public bool IsError;

			public bool IsFree;

			public Exception Error;

			public Action<Exception> ErrorHandle;

			public object SyncObject = new object();

			public TimerExecuter TimerThread;
		}

		private static bool _DEBUG = false;

		private static int _maxThread = 400;

		private static bool _runing = false;

		private static readonly Dictionary<Guid, TimerInfo> _dicTimers = new Dictionary<Guid, TimerInfo>();

		private static readonly AQueue<TimerInfo> _queueTimers = new AQueue<TimerInfo>();

		private static readonly AQueue<TimerExecuter> _queueIdleThreads = new AQueue<TimerExecuter>();

		private static readonly AQueue<TimerExecuter> _queueCompleteThreads = new AQueue<TimerExecuter>();

		private static readonly List<TimerExecuter> _timerThreads = new List<TimerExecuter>();

		private static long _lastSystemTick = 0L;

		private static long _TickStart = 0L;

		private static long _TickCount = 0L;

		public static long TickCount
		{
			get
			{
				if (_TickCount == 0)
				{
					_TickCount = DateTime.Now.Ticks / 10000;
				}
				if (_lastSystemTick == Environment.TickCount)
				{
					return _TickCount;
				}
				_lastSystemTick = Environment.TickCount;
				_TickCount = DateTime.Now.Ticks / 10000 - _TickStart;
				return _TickCount;
			}
		}

		public static Thread Call(Action action, string name)
		{
			Thread thread = new Thread(action.Invoke);
			if (name != null)
			{
				thread.Name = name;
			}
			thread.Start();
			return thread;
		}

		public static Thread ThreadCall(Action action) => ThreadCall(action, null);

		public static Thread ThreadCall(Action action, string name) => Call(action, name);

		public static Loop LoopCall(int interval, Action callback, Action<Exception> error)
		{
			Loop loop = new Loop(interval, callback, error);
			loop.Start();
			return loop;
		}

		public static void PoolCall(Action<object> handle, object value = null)
		{
			ThreadPool.QueueUserWorkItem(handle.Invoke, value);
		}

		public static void ThreadStop(Thread thread) => Stop(thread);

		public static void ThreadStop(Thread thread, object lockObj, int time)
		{
			bool flag = false;
			try
			{
				flag = Monitor.TryEnter(lockObj, time);
			}
			finally
			{
				if (flag)
				{
					Monitor.Exit(lockObj);
				}
			}
			Stop(thread);
		}

		public static void LoopStop(Loop loop)
		{
			loop.Stop();
		}

		public static void Stop(Thread thread)
		{
			try
			{
				thread.Abort();
			}
			catch
			{
			}
		}

		public static void Sleep(int ms = 10)
		{
			Thread.Sleep(ms);
		}

		public static Guid NewTimer(int interval, Action callback, Action<Exception> errorHandle)
		{
			return NewTimer(interval, callback, null, errorHandle);
		}

		public static Guid NewTimer(int interval, Action callback, string name, Action<Exception> errorHandle)
		{
			TimerInfo timerInfo = new TimerInfo();
			timerInfo.Interval = interval;
			timerInfo.Key = Guid.NewGuid();
			timerInfo.Callback = callback;
			timerInfo.ErrorHandle = errorHandle;
			timerInfo.IsError = false;
			timerInfo.IsReady = false;
			timerInfo.IsRuning = false;
			timerInfo.Name = name;
			TimerInfo timerInfo2 = timerInfo;
			NewTimer(timerInfo2);
			return timerInfo2.Key;
		}

		private static void NewTimer(TimerInfo info)
		{
			lock (_dicTimers)
			{
				_dicTimers[info.Key] = info;
				if (!_runing)
				{
					_runing = true;
					ThreadCall(Thread1);
					ThreadCall(Thread2);
				}
			}
		}

		public static void FreeTimer(Guid key, bool _stopThread = false)
		{
			TimerInfo timerInfo = null;
			lock (_dicTimers)
			{
				if (_dicTimers.ContainsKey(key))
				{
					timerInfo = _dicTimers[key];
					timerInfo.IsFree = true;
					_dicTimers.Remove(key);
				}
			}
			if (timerInfo != null && _stopThread)
			{
				lock (timerInfo.SyncObject)
				{
					if (timerInfo.TimerThread != null)
					{
						timerInfo.TimerThread.Reset();
					}
				}
			}
		}

		private static void Thread1()
		{
			while (true)
			{
				//bool flag = true;
				long tickCount = TickCount;
				lock (_dicTimers)
				{
					foreach (TimerInfo value in _dicTimers.Values)
					{
						lock (value.SyncObject)
						{
							if (!value.IsReady && !value.IsRuning && tickCount - value.LastEndTime > value.Interval)
							{
								value.IsReady = true;
								_queueTimers.Enqueue(value);
							}
						}
					}
				}
				long tickCount2 = TickCount;
				if (tickCount2 - tickCount < 5)
				{
					Thread.Sleep(5);
				}
			}
		}

		private static void Thread2()
		{
			//int num = 200;
			while (true)
			{
				bool flag = true;
				long tickCount = TickCount;
				while (_queueTimers.Count > 0)
				{
					TimerInfo timerInfo = null;
					lock (_queueTimers)
					{
						if (_queueTimers.Count > 0)
						{
							TimerInfo timerInfo2 = _queueTimers.Dequeue();
							if (timerInfo2 != null && _dicTimers.ContainsKey(timerInfo2.Key))
							{
								timerInfo = timerInfo2;
							}
						}
					}
					if (timerInfo == null)
					{
						continue;
					}
					TimerExecuter timerExecuter = null;
					while (true)
					{
						flag = true;
						timerExecuter = GetIdleThread();
						if (timerExecuter != null)
						{
							break;
						}
						Thread.Sleep(1);
					}
					if (timerInfo == null)
					{
						throw new Exception("item null");
					}
					timerExecuter.Run(timerInfo);
				}
				long tickCount2 = TickCount;
				if (tickCount2 - tickCount < 5)
				{
					Thread.Sleep(5);
				}
			}
		}

		private static void ThreadCompleted(TimerExecuter executer)
		{
			_ = _DEBUG;
		//	bool flag = 1 == 0;
			lock (_queueCompleteThreads)
			{
				_queueCompleteThreads.Enqueue(executer);
			}
		}

		private static void ThreadReset(TimerExecuter executer)
		{
			lock (_queueCompleteThreads)
			{
				if (!_queueCompleteThreads.Contains(executer))
				{
					_queueCompleteThreads.Enqueue(executer);
				}
			}
		}

		private static TimerExecuter GetIdleThread()
		{
			if (_queueIdleThreads.Count > 0)
			{
				return _queueIdleThreads.Dequeue();
			}
			while (_queueCompleteThreads.Count > 0)
			{
				TimerExecuter timerExecuter = _queueCompleteThreads.Peek();
				timerExecuter = _queueCompleteThreads.Dequeue();
				_queueIdleThreads.Enqueue(timerExecuter);
			}
			if (_queueIdleThreads.Count > 0)
			{
				return _queueIdleThreads.Dequeue();
			}
			if (_timerThreads.Count <= _maxThread)
			{
				return CreateThread();
			}
			return null;
		}

		private static TimerExecuter CreateThread()
		{
			TimerExecuter timerExecuter = new TimerExecuter(ThreadCompleted);
			_timerThreads.Add(timerExecuter);
			timerExecuter.Start();
			return timerExecuter;
		}
	}
}

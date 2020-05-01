using System;
using System.Collections.Concurrent;
using GB28181.Sys.Model;

namespace GB28181.Cache
{
    public class DeviceObjectCache : IMemoCache<Camera>
    {
        public string Name { get; set; }

        public event Action<object, Camera> OnItemAdded;

        public event Action<object, Camera> OnItemRemoved;


        /// <summary>
        /// Monitor Service For all Remote Node
        /// </summary>
        private ConcurrentDictionary<string, Camera> _cameraCollection = new ConcurrentDictionary<string, Camera>();


        public void Clear()
        {
            _cameraCollection.Clear();
        }

        public Camera FindOut(string deviceKey)
        {
            throw new NotImplementedException();
        }

        public Camera PlaceIn(string deviceKey, Camera value)
        {

            var result = _cameraCollection.AddOrUpdate(deviceKey, value, (k, v) => v);

            if (result != null)
            {
                OnItemAdded?.Invoke(this, value);
            }

            return result;
        }

        public bool Remove(string deviceKey)
        {

            var result = _cameraCollection.TryRemove(deviceKey, out Camera camera);

            if (result)
            {
                OnItemRemoved?.Invoke(this, camera);
            }
            return result;
        }

        public Camera TakeOut(string deviceKey)
        {

            var result = _cameraCollection.TryRemove(deviceKey, out Camera camera);

            if (result)
            {
                OnItemRemoved?.Invoke(this, camera);
            }

            return camera;
        }
    }
}

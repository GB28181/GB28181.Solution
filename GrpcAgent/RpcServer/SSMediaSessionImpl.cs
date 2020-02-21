using Grpc.Core;
using MediaContract;
using System.Threading.Tasks;
using SIPSorcery.GB28181.Servers;
using System;
using Logger4Net;
using System.Collections.Generic;

namespace GrpcAgent.WebsocketRpcServer
{
    public class SSMediaSessionImpl : VideoSession.VideoSessionBase
    {
        private static ILog logger = LogManager.GetLogger("RpcServer");
        private MediaEventSource _eventSource = null;
        private ISIPServiceDirector _sipServiceDirector = null;

        public SSMediaSessionImpl(MediaEventSource eventSource, ISIPServiceDirector sipServiceDirector)
        {
            _eventSource = eventSource;
            _sipServiceDirector = sipServiceDirector;
        }

        public override Task<KeepAliveReply> KeepAlive(KeepAliveRequest request, ServerCallContext context)
        {
            foreach (Dictionary<string, DateTime> dict in _sipServiceDirector.VideoSessionAlive)
            {
                if (dict.ContainsKey(request.Gbid + "," + request.Hdr.Sessionid))
                {
                    dict[request.Gbid + "," + request.Hdr.Sessionid] = DateTime.Now;
                }
            }
            var keepAliveReply = new KeepAliveReply()
            {
                Status = new MediaContract.Status()
                {
                    Code = 200,
                    Msg = "KeepAlive Successful!"
                }
            };
            return Task.FromResult(keepAliveReply);
        }

        public override Task<StartLiveReply> StartLive(StartLiveRequest request, ServerCallContext context)
        {
            try
            {
                logger.Debug("StartLive: request=" + request.ToString());
                _eventSource?.FileLivePlayRequestEvent(request, context);
                var reqeustProcessResult = _sipServiceDirector.RealVideoReq(request.Gbid, new int[] { request.Port }, request.Ipaddr);

                reqeustProcessResult?.Wait(System.TimeSpan.FromSeconds(1));

                //get the response .
                var resReply = new StartLiveReply()
                {
                    Ipaddr = reqeustProcessResult.Result.Item1,
                    Port = reqeustProcessResult.Result.Item2,
                    Hdr = GetHeaderBySipHeader(reqeustProcessResult.Result.Item3),
                    Status = new MediaContract.Status()
                    {
                        Code = 200,
                        Msg = "Request Successful!"
                    }
                };
                //add Video Session Alive
                Dictionary<string, DateTime> _Dictionary = new Dictionary<string, DateTime>();
                _Dictionary.Add(request.Gbid + ',' + resReply.Hdr.Sessionid, DateTime.Now);
                _sipServiceDirector.VideoSessionAlive.Add(_Dictionary);
                logger.Debug("StartLive: resReply=" + resReply.ToString());
                return Task.FromResult(resReply);
            }
            catch (Exception ex)
            {
                logger.Error("Exception GRPC StartLive: " + ex.Message);
                var resReply = new StartLiveReply()
                {
                    Status = new MediaContract.Status()
                    {
                        Msg = ex.Message
                    }
                };
                return Task.FromResult(resReply);
            }
        }

        public override Task<StartPlaybackReply> StartPlayback(StartPlaybackRequest request, ServerCallContext context)
        {
            try
            {
                logger.Debug("StartPlayback: request=" + request.ToString());
                _eventSource?.FilePlaybackRequestEvent(request, context);
                var reqeustProcessResult = _sipServiceDirector.BackVideoReq(request.Gbid, new int[] { request.Port }, request.Ipaddr, Convert.ToUInt64(request.BeginTime), Convert.ToUInt64(request.EndTime));
                reqeustProcessResult?.Wait(System.TimeSpan.FromSeconds(1));

                //get the response .
                var resReply = new StartPlaybackReply()
                {
                    Ipaddr = reqeustProcessResult.Result.Item1,
                    Port = reqeustProcessResult.Result.Item2,
                    Hdr = GetHeaderBySipHeader(reqeustProcessResult.Result.Item3),
                    Status = new MediaContract.Status()
                    {
                        Code = 200,
                        Msg = "Request Successful!"
                    }
                };
                //add Video Session Alive
                Dictionary<string, DateTime> _Dictionary = new Dictionary<string, DateTime>();
                _Dictionary.Add(request.Gbid + ',' + resReply.Hdr.Sessionid, DateTime.Now);
                _sipServiceDirector.VideoSessionAlive.Add(_Dictionary);
                logger.Debug("StartPlayback: resReply=" + resReply.ToString());
                return Task.FromResult(resReply);
            }
            catch (Exception ex)
            {
                logger.Error("Exception GRPC StartPlayback: " + ex.Message);
                var resReply = new StartPlaybackReply()
                {
                    Status = new MediaContract.Status()
                    {
                        Msg = ex.Message
                    }
                };
                return Task.FromResult(resReply);
            }
        }

        //public override Task<VideoDownloadReply> VideoDownload(VideoDownloadRequest request, ServerCallContext context)
        //{
        //    try
        //    {
        //        _eventSource?.VideoDownloadRequestEvent(request, context);
        //        var reqeustProcessResult = _sipServiceDirector.VideoDownloadReq(Convert.ToDateTime(request.BeginTime), Convert.ToDateTime(request.EndTime), request.Gbid, new int[] { request.Port }, request.Ipaddr);

        //        reqeustProcessResult?.Wait(System.TimeSpan.FromSeconds(1));

        //        //get the response .
        //        var resReply = new VideoDownloadReply()
        //        {
        //            Ipaddr = reqeustProcessResult.Result.Item1,
        //            Port = reqeustProcessResult.Result.Item2,
        //            Hdr = GetHeaderBySipHeader(reqeustProcessResult.Result.Item3),
        //            Status = new MediaContract.Status()
        //            {
        //                Code = 200,
        //                Msg = "Request Successful!"
        //            }
        //        };
        //        //add Video Session Alive
        //        Dictionary<string, DateTime> _Dictionary = new Dictionary<string, DateTime>();
        //        _Dictionary.Add(request.Gbid + ',' + resReply.Hdr.Sessionid, DateTime.Now);
        //        _sipServiceDirector.VideoSessionAlive.Add(_Dictionary);
        //        return Task.FromResult(resReply);
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error("Exception GRPC VideoDownloadReply: " + ex.Message);
        //        var resReply = new VideoDownloadReply()
        //        {
        //            Status = new MediaContract.Status()
        //            {
        //                Msg = ex.Message
        //            }
        //        };
        //        return Task.FromResult(resReply);
        //    }
        //}

        public override Task<StopReply> Stop(StopRequest request, ServerCallContext context)
        {
            bool tf = false;
            string msg = "";
            try
            {
                switch (request.BusinessType)
                {
                    case BusinessType.BtLiveplay:
                        tf = _sipServiceDirector.Stop(string.IsNullOrEmpty(request.Gbid) ? "42010000001180000184" : request.Gbid, request.Hdr.Sessionid);
                        break;
                    case BusinessType.BtPlayback:
                        tf = _sipServiceDirector.BackVideoStopPlayingControlReq(string.IsNullOrEmpty(request.Gbid) ? "42010000001180000184" : request.Gbid, request.Hdr.Sessionid);
                        break;
                    default:
                        tf = _sipServiceDirector.Stop(string.IsNullOrEmpty(request.Gbid) ? "42010000001180000184" : request.Gbid, request.Hdr.Sessionid);
                        break;
                }
                msg = tf ? "Stop Successful!" : "Stop Failed!";
                var reply = new StopReply()
                {
                    Status = new MediaContract.Status()
                    {
                        Code = 200,
                        Msg = msg
                    }
                };
                return Task.FromResult(reply);
            }
            catch (Exception ex)
            {
                logger.Error("Exception GRPC StopVideo: " + ex.Message);
                var reply = new StopReply()
                {
                    Status = new MediaContract.Status()
                    {
                        Code = 400,
                        Msg = ex.Message
                    }
                };
                return Task.FromResult(reply);
            }
        }

        private Header GetHeaderBySipHeader(SIPSorcery.GB28181.SIP.SIPHeader sipHeader)
        {
            Header header = new Header();
            header.Sequence = sipHeader.CSeq;
            header.Sessionid = sipHeader.CallId;
            //header.Version = sipHeader.CSeq + sipHeader.CallId;
            return header;
        }

        public override Task<PlaybackControlReply> PlaybackControl(PlaybackControlRequest request, ServerCallContext context)
        {
            bool tf = false;
            string msg = "";
            try
            {
                switch (request.PlaybackType)
                {
                    case PlaybackControlType.Moveto:
                        tf = _sipServiceDirector.BackVideoPlayPositionControlReq(string.IsNullOrEmpty(request.Gbid) ? "42010000001180000184" : request.Gbid, request.Hdr.Sessionid, request.StartTime);
                        break;
                    case PlaybackControlType.Pause:
                        tf = _sipServiceDirector.BackVideoPauseControlReq(string.IsNullOrEmpty(request.Gbid) ? "42010000001180000184" : request.Gbid, request.Hdr.Sessionid);
                        break;
                    case PlaybackControlType.Resume:
                        tf = _sipServiceDirector.BackVideoContinuePlayingControlReq(string.IsNullOrEmpty(request.Gbid) ? "42010000001180000184" : request.Gbid, request.Hdr.Sessionid);
                        break;
                    case PlaybackControlType.Scale:
                        tf = _sipServiceDirector.BackVideoPlaySpeedControlReq(string.IsNullOrEmpty(request.Gbid) ? "42010000001180000184" : request.Gbid, request.Hdr.Sessionid, request.Scale);
                        break;
                    default:
                        tf = _sipServiceDirector.Stop(string.IsNullOrEmpty(request.Gbid) ? "42010000001180000184" : request.Gbid, request.Hdr.Sessionid);
                        break;
                }
                msg = tf ? "PlaybackControl Successful!" : "PlaybackControl Failed!";
                var reply = new PlaybackControlReply()
                {
                    Status = new MediaContract.Status()
                    {
                        Code = 200,
                        Msg = msg
                    }
                };
                return Task.FromResult(reply);
            }
            catch (Exception ex)
            {
                logger.Error("Exception GRPC PlaybackControl: " + ex.Message);
                var reply = new PlaybackControlReply()
                {
                    Status = new MediaContract.Status()
                    {
                        Code = 400,
                        Msg = ex.Message
                    }
                };
                return Task.FromResult(reply);
            }
        }
    }
}

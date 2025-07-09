// Copyright (c) coherence ApS.
// See the license file in the package root for more information.

namespace Coherence.Flux
{
    using System;
    using System.Net;

    /*
     * Class that represents an async read from System.Net.Sockets.UdpClient.
     */
    public class UdpAsyncRead
    {
        private readonly System.Net.Sockets.UdpClient Udp;
        private readonly PortReceivedDataCallback UserCallback;
        private readonly object UserData;
        private readonly AsyncCallback ReceiveCallback;
        private byte[] ReceivedBytes;
        private IPEndPoint ReceivedFrom;

        public UdpAsyncRead(System.Net.Sockets.UdpClient udp, PortReceivedDataCallback userCallback, object userData, AsyncCallback receiveCallback)
        {
            Udp = udp;
            UserCallback = userCallback;
            UserData = userData;
            ReceiveCallback = receiveCallback;
        }

        public void Begin()
        {
            ResetToBeginRead();
            Udp.BeginReceive(ReceiveCallback, this);
        }

        public void End(IAsyncResult ar)
        {
            ReceivedFrom = new IPEndPoint(IPAddress.Any, 0);
            ReceivedBytes = Udp.EndReceive(ar, ref ReceivedFrom);
        }

        public void Report()
        {
            UserCallback(ReceivedBytes, ReceivedFrom, UserData);
        }

        /*
         * Only resets the data that changes per read.
         */
        private void ResetToBeginRead()
        {
            ReceivedBytes = null;
            ReceivedFrom = null;
        }
    }
}

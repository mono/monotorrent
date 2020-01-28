//
// Block.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;

using MonoTorrent.Client.PiecePicking;

namespace MonoTorrent.Client
{
    /// <summary>
    ///
    /// </summary>
    public struct Block
    {
        #region Private Fields

        private readonly Piece piece;
        private bool requested;
        private bool received;

        #endregion Private Fields


        #region Properties

        public int PieceIndex {
            get { return this.piece.Index; }
        }

        public bool Received {
            get { return this.received; }
            internal set {
                if (value && !received)
                    piece.TotalReceived++;

                else if (!value && received)
                    piece.TotalReceived--;

                this.received = value;
            }
        }

        public bool Requested {
            get { return this.requested; }
            private set {
                if (value && !requested)
                    piece.TotalRequested++;

                else if (!value && requested)
                    piece.TotalRequested--;

                this.requested = value;
            }
        }

        public int RequestLength { get; }

        public bool RequestTimedOut {
            get { // 60 seconds timeout for a request to fulfill
                return !Received && RequestedOff != null && RequestedOff.TimeSinceLastMessageReceived > TimeSpan.FromMinutes (1);
            }
        }

        internal IPieceRequester RequestedOff { get; private set; }

        public int StartOffset { get; }

        #endregion Properties


        #region Constructors

        internal Block (Piece piece, int startOffset, int requestLength)
        {
            this.RequestedOff = null;
            this.piece = piece;
            this.received = false;
            this.requested = false;
            this.RequestLength = requestLength;
            this.StartOffset = startOffset;
        }

        #endregion


        #region Methods

        internal PieceRequest CreateRequest (IPieceRequester peer)
        {
            Requested = true;
            RequestedOff = peer;
            RequestedOff.AmRequestingPiecesCount++;
            return new PieceRequest (PieceIndex, StartOffset, RequestLength);
        }

        internal void CancelRequest ()
        {
            Requested = false;
            RequestedOff.AmRequestingPiecesCount--;
            RequestedOff = null;
        }

        public override bool Equals (object obj)
        {
            if (!(obj is Block other))
                return false;

            return this.PieceIndex == other.PieceIndex && this.StartOffset == other.StartOffset && this.RequestLength == other.RequestLength;
        }

        public override int GetHashCode ()
        {
            return this.PieceIndex ^ this.RequestLength ^ this.StartOffset;
        }

        internal static int IndexOf (Block[] blocks, int startOffset, int blockLength)
        {
            var index = startOffset / Piece.BlockSize;
            if (blocks[index].StartOffset != startOffset || blocks[index].RequestLength != blockLength)
                return -1;
            return index;
        }

        #endregion
    }
}

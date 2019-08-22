//
// EndGamePickerTests.cs
//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2008 Alan McGovern
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
using System.Collections.Generic;
using System.Linq;

using MonoTorrent.Client.Encryption;
using MonoTorrent.Client.PiecePicking;

using NUnit.Framework;

namespace MonoTorrent.Client
{
    [TestFixture]
    public class EndGamePickerTests
    {
        BitField bitfield;
        PeerId id;
        PeerId other;
        EndGamePicker picker;
        List<Piece> pieces;
        TestRig rig;

        [OneTimeSetUp]
        public void FixtureSetup()
        {
            rig = TestRig.CreateMultiFile();
        }

        [SetUp]
        public void Setup()
        {
            bitfield = new BitField(40).SetAll(true)
                                       .Set(4, false)
                                       .Set(6, false)
                                       .Set(24, false)
                                       .Set(36, false);
            picker = new EndGamePicker();
            pieces = new List<Piece>(new Piece[] { 
                new Piece(4, rig.Torrent.PieceLength, rig.Torrent.Size),
                new Piece(6, rig.Torrent.PieceLength, rig.Torrent.Size),
                new Piece(24, rig.Torrent.PieceLength, rig.Torrent.Size),
                new Piece(36, rig.Torrent.PieceLength, rig.Torrent.Size)
            });

            id = new PeerId(new Peer("peerid", new Uri("ipv4://weburl.com")), rig.Manager, NullConnection.Incoming);
            id.Encryptor = id.Decryptor = PlainTextEncryption.Instance;
            id.IsChoking = false;
            id.BitField.SetAll(false);

            other = new PeerId(new Peer("other", new Uri("ipv4://other.com")), rig.Manager, NullConnection.Incoming);
            other.Decryptor = other.Encryptor = PlainTextEncryption.Instance;
            other.IsChoking = false;
            other.BitField.SetAll(false);
        }

        [OneTimeTearDown]
        public void FixtureTeardown()
        {
            rig.Dispose();
        }

        [Test]
        public void CancelTest()
        {
            foreach (Piece p in pieces)
            {
                for (int i = 0; i < p.BlockCount; i++)
                {
                    if (i % 2 == 0)
                        p.Blocks[i].CreateRequest(id);
                    else
                        p.Blocks[i].CreateRequest(other);
                }
            }

            picker.Initialise(bitfield, rig.Manager.Torrent.Files, pieces);
            picker.CancelRequests(id);
            picker.CancelRequests(other);

            id.BitField[4] = true;
            Assert.IsNotNull(picker.PickPiece(id, new List<PeerId>()));
        }

        [Test]
        public void MultiPick()
        {
            id.BitField.Set(pieces[0].Index, true);
            other.BitField.Set(pieces[0].Index, true);

            for (int i = 2; i < pieces[0].BlockCount; i++)
            {
                pieces[0].Blocks[i].CreateRequest (new PeerId (new Peer ("", new Uri ("http://asd")), rig.Manager, NullConnection.Incoming));
                pieces[0].Blocks[i].Received = true;
            }
            
            picker.Initialise(bitfield, rig.Torrent.Files, pieces);

            // Pick blocks 1 and 2 for both peers
            while (picker.PickPiece(id, new List<PeerId>()) != null) ;
            while (picker.PickPiece(other, new List<PeerId>()) != null) ;

            Assert.AreEqual(2, id.AmRequestingPiecesCount, "#1");
            Assert.AreEqual(2, other.AmRequestingPiecesCount, "#1");

            Piece piece;
            if (!picker.ValidatePiece(id, pieces[0].Index, pieces[0][0].StartOffset, pieces[0][0].RequestLength, out piece))
                Assert.Fail("I should've validated!");

            if (picker.ValidatePiece(other, pieces[0].Index, pieces[0][0].StartOffset, pieces[0][0].RequestLength, out piece))
                Assert.Fail("I should not have validated!");

            Assert.AreEqual(1, id.AmRequestingPiecesCount, "#1");
            Assert.AreEqual(1, other.AmRequestingPiecesCount, "#1");
            Assert.IsTrue(pieces[0][0].Received, "#5");
            Assert.AreEqual(16, pieces[0].TotalRequested, "#6");
            Assert.AreEqual(15, pieces[0].TotalReceived, "#7");
        }

        [Test]
        public void HashFail()
        {
            Piece piece;
            PieceRequest m;
            List<PieceRequest> requests = new List<PieceRequest>();

            id.BitField[0] = true;
            picker.Initialise(rig.Manager.Bitfield, rig.Torrent.Files, new List<Piece>());

            while ((m = picker.PickPiece(id, new List<PeerId>())) != null)
                requests.Add(m);

            foreach (var message in requests)
                Assert.IsTrue(picker.ValidatePiece(id, message.PieceIndex, message.StartOffset, message.RequestLength, out piece));

            Assert.IsNotNull(picker.PickPiece(id, new List<PeerId>()));
        }

        [Test]
        public void ReceivedPiecesAreNotRequested()
        {
            for (int i = 2; i < pieces[0].BlockCount; i++) {
                pieces[0].Blocks[i].CreateRequest (new PeerId (new Peer ("", new Uri ("http://asd")), rig.Manager, NullConnection.Incoming));
                pieces[0].Blocks[i].Received = true;
            }

            picker.Initialise(bitfield, rig.Torrent.Files, pieces);
            Assert.IsTrue (picker.Requests.All (t => !t.Block.Received), "#1");
        }
    }
}

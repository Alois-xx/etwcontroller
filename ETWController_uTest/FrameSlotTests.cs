using ETWController.Screenshots;
using System;
using System.Collections.Generic;
using System.Drawing;
using Xunit;

namespace ETWController_uTest
{
    public class FrameSlotTests
    {
        /// <summary>
        /// The slot hands out at most one frame. While the writer owns a buffer the next screenshot
        /// must be dropped instead of buffered, otherwise frames pile up when captures come in faster
        /// than the system can encode them.
        /// </summary>
        [Fact]
        public void FrameIsDroppedWhileTheWriterOwnsTheBuffer()
        {
            using (var slot = new FrameSlot(64, 32))
            {
                Bitmap captureBuffer = slot.BeginFrame();
                Assert.NotNull(captureBuffer);
                Assert.True(slot.EndFrame("1", DateTime.Now));

                // Writer owns the frame, so this screenshot has to be dropped, not queued.
                Assert.Null(slot.BeginFrame());

                FrameTake frame = slot.Acquire();
                Assert.NotNull(frame);
                slot.Release();

                // After the writer is done, a buffer is available again and it is not the one that
                // is currently being saved - the captured content must not be overwritten.
                Bitmap next = slot.BeginFrame();
                Assert.NotNull(next);
                Assert.NotSame(next, frame.Buffer);
            }
        }

        /// <summary>
        /// Buffers are reused across frames instead of being allocated per frame.
        /// </summary>
        [Fact]
        public void BuffersAreReusedAcrossManyFrames()
        {
            using (var slot = new FrameSlot(64, 32))
            {
                HashSet<Bitmap> buffers = new HashSet<Bitmap>();

                for (int i = 0; i < 20; i++)
                {
                    Bitmap captureBuffer = slot.BeginFrame();
                    Assert.NotNull(captureBuffer);
                    buffers.Add(captureBuffer);

                    Assert.True(slot.EndFrame(i.ToString(), DateTime.Now));

                    FrameTake frame = slot.Acquire();
                    Assert.NotNull(frame);
                    slot.Release();
                }

                Assert.Equal(2, buffers.Count);   // no buffer is allocated per frame
                Assert.Equal(2, slot.BufferCount);
            }
        }

        /// <summary>
        /// A layout change may only resize the buffer that is not being saved, otherwise the captured
        /// frame is lost.
        /// </summary>
        [Fact]
        public void EnsureSizeReallocatesOnlyTheFreeBuffer()
        {
            using (var slot = new FrameSlot(64, 32))
            {
                Bitmap first = slot.BeginFrame();
                Assert.True(slot.EndFrame("1", DateTime.Now));

                FrameTake frame = slot.Acquire();
                Assert.NotNull(frame);

                Assert.True(slot.EnsureSize(128, 64));
                Assert.NotSame(first, slot.CaptureBuffer);

                // The frame the writer is saving keeps its original size.
                Assert.Equal(new Size(64, 32), frame.Buffer.Size);

                slot.Release();
                Assert.Equal(new Size(128, 64), slot.CaptureBuffer.Size);
            }
        }
    }
}

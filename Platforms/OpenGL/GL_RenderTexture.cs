﻿using Foster.Framework;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Foster.OpenGL
{
    internal class GL_RenderTexture : RenderTexture.Platform
    {

        private readonly GL_Graphics graphics;
        private readonly Dictionary<ISystemOpenGL.Context, uint> framebuffers = new Dictionary<ISystemOpenGL.Context, uint>();

        internal GL_RenderTexture(GL_Graphics graphics, int width, int height, TextureFormat[] colorAttachmentFormats, TextureFormat depthFormat)
        {
            this.graphics = graphics;

            // texture (color) attachments
            for (int i = 0; i < colorAttachmentFormats.Length; i++)
            {
                var attachment = new Texture(graphics, width, height, colorAttachmentFormats[i]);
                var glTexture = (GL_Texture)attachment.Internal;
                glTexture.Flip = true;
                Attachments.Add(attachment);
            }

            if (depthFormat != TextureFormat.None)
            {
                Depth = new Texture(graphics, width, height, depthFormat);
                var glTexture = (GL_Texture)Depth.Internal;
                glTexture.Flip = true;
            }
        }

        ~GL_RenderTexture()
        {
            Dispose();
        }

        public void Bind(ISystemOpenGL.Context context)
        {
            // create new framebuffer if it's needed
            // frame buffers are not shared between contexts
            if (!framebuffers.TryGetValue(context, out uint id))
            {
                id = GL.GenFramebuffer();

                GL.BindFramebuffer(GLEnum.FRAMEBUFFER, id);

                // color attachments
                int i = 0;
                foreach (Texture texture in Attachments)
                {
                    if (texture.Internal is GL_Texture glTexture)
                    {
                        GL.FramebufferTexture2D(GLEnum.FRAMEBUFFER, GLEnum.COLOR_ATTACHMENT0 + i, GLEnum.TEXTURE_2D, glTexture.ID, 0);
                        i++;
                    }
                }

                // depth stencil attachment
                if (Depth != null && Depth.Internal is GL_Texture glDepthTexture)
                {
                    GL.FramebufferRenderbuffer(GLEnum.FRAMEBUFFER, GLEnum.DEPTH_STENCIL_ATTACHMENT, GLEnum.RENDERBUFFER, glDepthTexture.ID);
                }

                framebuffers.Add(context, id);
            }
            else
            {
                GL.BindFramebuffer(GLEnum.FRAMEBUFFER, id);
            }
        }

        protected override void Dispose()
        {
            if (framebuffers.Count > 0)
            {
                foreach (var kv in framebuffers)
                    graphics.GetContextMeta(kv.Key).FrameBuffersToDelete.Add(kv.Value);
                framebuffers.Clear();
            }
        }

    }
}

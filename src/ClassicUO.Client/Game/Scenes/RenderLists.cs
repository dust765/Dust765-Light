// SPDX-License-Identifier: BSD-2-Clause
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ClassicUO.Game.Scenes
{
    /// <summary>
    /// A queued draw into the gump layer. Prefer the typed text path: store a reference to
    /// the <see cref="RenderedText"/> plus its draw parameters. That avoids allocating a
    /// closure per text per frame and makes it safe to skip entries whose text was
    /// destroyed or returned to the <see cref="RenderedText"/> pool between queue and flush.
    ///
    /// Callers that need an arbitrary draw (clipping, compound operations, solid color
    /// rectangles, atlas sprites) use the <see cref="Callback"/> path.
    /// </summary>
    internal readonly struct GumpCommand
    {
        public readonly RenderedText Text;
        public readonly int X;
        public readonly int Y;
        public readonly float LayerDepth;
        public readonly float Alpha;
        public readonly ushort Hue;
        public readonly Func<UltimaBatcher2D, bool> Callback;

        public GumpCommand(RenderedText text, int x, int y, float layerDepth, float alpha, ushort hue)
        {
            Text = text;
            X = x;
            Y = y;
            LayerDepth = layerDepth;
            Alpha = alpha;
            Hue = hue;
            Callback = null;
        }

        public GumpCommand(Func<UltimaBatcher2D, bool> callback)
        {
            Text = null;
            X = 0;
            Y = 0;
            LayerDepth = 0f;
            Alpha = 0f;
            Hue = 0;
            Callback = callback;
        }
    }

    /// <summary>
    /// Represents an ordered queue of GameObjects to be rendered.
    /// The order is determined by the draw order, not by the insertion order.
    /// Implementation for sorting and processing is passed as delegates.
    /// </summary>
    internal class RenderLists
    {
        private static readonly Comparer<GameObject> EffectDistanceComparer =
            Comparer<GameObject>.Create(static (a, b) => a.Distance.CompareTo(b.Distance));

        private static readonly Comparer<GameObject> EffectDepthComparer = Comparer<GameObject>.Create(
            static (a, b) => a.CalculateDepthZ().CompareTo(b.CalculateDepthZ())
        );

        private readonly List<GameObject> _tiles = [];
        private readonly List<GameObject> _stretchedTiles = [];
        private readonly List<GameObject> _statics = [];
        private readonly List<GameObject> _animations = [];
        private readonly List<GameObject> _effects = [];
        private readonly List<GameObject> _transparentObjects = [];
        // Atlas and non-atlas gump elements share one queue so they keep insertion order:
        // splitting them draws every text element on top of every sprite, hiding controls.
        private readonly List<GumpCommand> _gumpLayers = [];
        private GameObject[] _effectCapScratch = [];

        public void Clear()
        {
            _tiles.Clear();
            _stretchedTiles.Clear();
            _statics.Clear();
            _animations.Clear();
            _effects.Clear();
            _transparentObjects.Clear();
            _gumpLayers.Clear();
        }

        public void Add(GameObject toRender, bool isTransparent = false)
        {
            if (isTransparent)
            {
                _transparentObjects.Add(toRender);
                return;
            }

            switch (toRender)
            {
                case Land land:
                    if (land.IsStretched)
                    {
                        _stretchedTiles.Add(toRender);
                    }
                    else
                    {
                        _tiles.Add(toRender);
                    }
                    break;

                case Static:
                case Multi:
                    _statics.Add(toRender);
                    break;

                case Mobile:
                    _animations.Add(toRender);
                    break;

                case Item item:
                    if (item.IsCorpse)
                    {
                        _animations.Add(toRender);
                    }
                    else
                    {
                        _statics.Add(toRender);
                    }
                    break;

                case GameEffect:
                    _effects.Add(toRender);
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// This is an intermediate, crappy solution. Rewriting gump rendering would be way too much at this point.
        /// Adding gump elements that use atlas textures for efficient rendering.
        /// </summary>
        /// <param name="toRender"></param>
        public void AddGumpWithAtlas(Func<UltimaBatcher2D, bool> toRender)
        {
            if (toRender == null)
            {
                return;
            }

            _gumpLayers.Add(new GumpCommand(toRender));
        }

        /// <summary>
        /// Queue a <see cref="RenderedText"/> draw. This is the preferred path for text:
        /// allocation-free, insertion order preserved alongside the closure entries, and
        /// flushed with a guard against destroyed or recycled text references.
        /// </summary>
        public void AddGumpNoAtlas(RenderedText text, int x, int y, float layerDepth, float alpha = 1f, ushort hue = 0)
        {
            if (text == null)
            {
                return;
            }

            _gumpLayers.Add(new GumpCommand(text, x, y, layerDepth, alpha, hue));
        }

        /// <summary>
        /// Fallback: queue an arbitrary draw closure. Use this for compound operations
        /// (clipping, nested render lists, solid-color rectangles) that do not fit the
        /// <see cref="RenderedText"/> fast path. New code drawing text should prefer the
        /// typed overload.
        /// </summary>
        public void AddGumpNoAtlas(Func<UltimaBatcher2D, bool> toRender)
        {
            if (toRender == null)
            {
                return;
            }

            _gumpLayers.Add(new GumpCommand(toRender));
        }

        public int DrawRenderLists(UltimaBatcher2D batcher, sbyte maxGroundZ)
        {
            int result = DrawRenderList(batcher, _tiles, maxGroundZ) +
                   DrawRenderList(batcher, _stretchedTiles, maxGroundZ) +
                   DrawRenderList(batcher, _statics, maxGroundZ) +
                   DrawRenderList(batcher, _animations, maxGroundZ) +
                   DrawRenderList(batcher, _effects, maxGroundZ);

            result += DrawOverlays(batcher, maxGroundZ);

            return result;
        }

        /// <summary>
        /// Chunk-mesh path: land and statics that were batched into per-chunk GPU buffers are
        /// drawn straight from those buffers, and only the objects excluded from the mesh
        /// (animated water, foliage, trees, rocks, fading and gradient-CoT objects) still go
        /// through the per-object CPU lists.
        /// </summary>
        public int DrawRenderLists(
            UltimaBatcher2D batcher,
            sbyte maxGroundZ,
            List<Chunk> visibleChunks,
            int offsetX,
            int offsetY
        )
        {
            int result = 0;

            foreach (Chunk chunk in visibleChunks)
            {
                ChunkMesh mesh = chunk.Mesh;

                if (mesh.Land.Count > 0)
                {
                    mesh.Land.BuildVisibleIndices();
                }

                if (mesh.Statics.Count > 0)
                {
                    mesh.Statics.BuildVisibleIndices();
                }
            }

            batcher.SetWorldOffset(offsetX, offsetY);

            foreach (Chunk chunk in visibleChunks)
            {
                result += DrawMeshLayer(batcher, chunk.Mesh.Land);
            }

            batcher.ResetWorldOffset();

            result += DrawRenderList(batcher, _tiles, maxGroundZ);
            result += DrawRenderList(batcher, _stretchedTiles, maxGroundZ);

            batcher.SetWorldOffset(offsetX, offsetY);

            foreach (Chunk chunk in visibleChunks)
            {
                result += DrawMeshLayer(batcher, chunk.Mesh.Statics);
            }

            batcher.ResetWorldOffset();

            result += DrawRenderList(batcher, _statics, maxGroundZ) +
                   DrawRenderList(batcher, _animations, maxGroundZ) +
                   DrawRenderList(batcher, _effects, maxGroundZ);

            result += DrawOverlays(batcher, maxGroundZ);

            return result;
        }

        private static int DrawMeshLayer(UltimaBatcher2D batcher, MeshLayer layer)
        {
            if (layer.VisibleSpriteCount == 0 || layer.VertexBuffer == null || layer.VertexBuffer.IsDisposed)
            {
                return 0;
            }

            layer.FlushAlphaChanges();

            DynamicIndexBuffer indexBuffer = batcher.GetDynamicIndexBuffer(layer.VisibleSpriteCount * 6);
            layer.UploadVisibleIndices(indexBuffer);

            batcher.GraphicsDevice.SetVertexBuffer(layer.VertexBuffer);
            batcher.GraphicsDevice.Indices = indexBuffer;

            for (int i = 0; i < layer.VisibleRunCount; i++)
            {
                ref var run = ref layer.VisibleRuns[i];
                batcher.DrawDirectIndexed(run.Texture, run.Start * 6, run.Count * 2, layer.Count * 4);
            }

            return layer.VisibleSpriteCount;
        }

        private int DrawOverlays(UltimaBatcher2D batcher, sbyte maxGroundZ)
        {
            if (_transparentObjects.Count == 0 && _gumpLayers.Count == 0)
            {
                return 0;
            }

            batcher.SetStencil(DepthStencilState.DepthRead);

            int result = DrawRenderList(batcher, _transparentObjects, maxGroundZ)
                + DrawGumpLayers(batcher, _gumpLayers);

            batcher.SetStencil(null);

            return result;
        }

        private static int DrawRenderList(UltimaBatcher2D batcher, List<GameObject> renderList, sbyte maxGroundZ)
        {
            int done = 0;

            foreach (var obj in renderList)
            {
                if (obj.Z <= maxGroundZ)
                {
                    float depth = obj.CalculateDepthZ();

                    if (obj.Draw(batcher, obj.RealScreenPosition.X, obj.RealScreenPosition.Y, depth))
                    {
                        done++;
                    }
                }
            }

            return done;
        }

        private int DrawEffectsCapped(UltimaBatcher2D batcher, sbyte maxGroundZ, int maxSprites)
        {
            if (maxSprites <= 0 || _effects.Count <= maxSprites)
            {
                return DrawRenderList(batcher, _effects, maxGroundZ);
            }

            int n = _effects.Count;

            if (_effectCapScratch.Length < n)
            {
                Array.Resize(ref _effectCapScratch, Math.Max(n, _effectCapScratch.Length * 2));
            }

            for (int i = 0; i < n; i++)
            {
                _effectCapScratch[i] = _effects[i];
            }

            Array.Sort(_effectCapScratch, 0, n, EffectDistanceComparer);
            Array.Sort(_effectCapScratch, 0, maxSprites, EffectDepthComparer);

            int done = 0;

            for (int i = 0; i < maxSprites; i++)
            {
                GameObject obj = _effectCapScratch[i];

                if (obj.Z <= maxGroundZ)
                {
                    float depth = obj.CalculateDepthZ();

                    if (obj.Draw(batcher, obj.RealScreenPosition.X, obj.RealScreenPosition.Y, depth))
                    {
                        done++;
                    }
                }
            }

            return done;
        }

        private static int DrawGumpLayers(UltimaBatcher2D batcher, List<GumpCommand> renderList)
        {
            int done = 0;

            // AsSpan avoids the List<T> enumerator allocation on the hot path.
            Span<GumpCommand> span = CollectionsMarshal.AsSpan(renderList);

            for (int i = 0; i < span.Length; i++)
            {
                ref readonly GumpCommand cmd = ref span[i];

                if (cmd.Text != null)
                {
                    // HasContent rejects destroyed or empty text, which happens when the
                    // instance went back to the pool between queue and flush.
                    if (!cmd.Text.HasContent)
                    {
                        continue;
                    }

                    if (cmd.Text.Draw(batcher, cmd.X, cmd.Y, cmd.LayerDepth, cmd.Alpha, cmd.Hue))
                    {
                        done++;
                    }
                }
                else if (cmd.Callback != null && cmd.Callback.Invoke(batcher))
                {
                    done++;
                }
            }

            return done;
        }
    }
}

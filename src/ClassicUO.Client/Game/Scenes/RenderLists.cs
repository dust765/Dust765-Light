// SPDX-License-Identifier: BSD-2-Clause
using ClassicUO.Game;
using ClassicUO.Game.GameObjects;
using ClassicUO.Game.Map;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace ClassicUO.Game.Scenes
{
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
        private readonly List<Func<UltimaBatcher2D, bool>> _gumpLayers = [];
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
            _gumpLayers.Add(toRender);
        }

        /// <summary>
        /// Adding gump elements that do not use atlas textures and will be rendered separately.
        /// </summary>
        /// <param name="toRender"></param>
        public void AddGumpNoAtlas(Func<UltimaBatcher2D, bool> toRender)
        {
            _gumpLayers.Add(toRender);
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

        private static int DrawGumpLayers(UltimaBatcher2D batcher, List<Func<UltimaBatcher2D, bool>> renderList)
        {
            int done = 0;

            foreach (var obj in renderList)
            {
                if (obj.Invoke(batcher))
                {
                    done++;
                }
            }

            return done;
        }
    }
}

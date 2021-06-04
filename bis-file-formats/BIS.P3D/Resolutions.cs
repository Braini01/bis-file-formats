using System;
using System.Collections.Generic;
using System.Text;

namespace BIS.P3D
{
    public enum LodName
    {
        ViewGunner,
        ViewPilot,
        ViewCargo,
        Geometry,
        Memory,
        LandContact,
        Roadway,
        Paths,
        HitPoints,
        ViewGeometry,
        FireGeometry,
        ViewCargoGeometry,
        ViewCargoFireGeometry,
        ViewCommander,
        ViewCommanderGeometry,
        ViewCommanderFireGeometry,
        ViewPilotGeometry,
        ViewPilotFireGeometry,
        ViewGunnerGeometry,
        ViewGunnerFireGeometry,
        SubParts,
        ShadowVolumeViewCargo,
        ShadowVolumeViewPilot,
        ShadowVolumeViewGunner,
        Wreck,
        PhysX,
        ShadowVolume,
        Resolution,
        Undefined
    }

    public static class Resolution
    {
        private const float specialLod = 1e15f;

        public const float GEOMETRY = 1e13f;
        public const float BUOYANCY = 2e13f;
        public const float PHYSXOLD = 3e13f;
        public const float PHYSX = 4e13f;

        public const float MEMORY = 1e15f;
        public const float LANDCONTACT = 2e15f;
        public const float ROADWAY = 3e15f;
        public const float PATHS = 4e15f;
        public const float HITPOINTS = 5e15f;

        public const float VIEW_GEOMETRY = 6e15f;
        public const float FIRE_GEOMETRY = 7e15f;

        public const float VIEW_GEOMETRY_CARGO = 8e15f;
        public const float VIEW_GEOMETRY_PILOT = 13e15f;
        public const float VIEW_GEOMETRY_GUNNER = 15e15f;
        public const float FIRE_GEOMETRY_GUNNER = 16e15f;

        public const float SUBPARTS = 17e15f;

        public const float SHADOWVOLUME_CARGO = 18e15f;
        public const float SHADOWVOLUME_PILOT = 19e15f;
        public const float SHADOWVOLUME_GUNNER = 20e15f;

        public const float WRECK = 21e15f;

        public const float VIEW_COMMANDER = 10e15f;
        public const float VIEW_GUNNER = 1000f;
        public const float VIEW_PILOT = 1100f;
        public const float VIEW_CARGO = 1200f;

        public const float SHADOWVOLUME = 10000.0f;
        public const float SHADOWBUFFER = 11000.0f;

        public const float SHADOW_MIN = 10000.0f;
        public const float SHADOW_MAX = 20000.0f;

        /// <summary>
        /// Tells us if the current LOD with given resolution has normal NamedSelections (returns true) or empty ones (return false)
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static bool KeepsNamedSelections(float r)
        {
            return r == MEMORY || r == FIRE_GEOMETRY || r == GEOMETRY
                || r == VIEW_GEOMETRY || r == VIEW_GEOMETRY_PILOT || r == VIEW_GEOMETRY_GUNNER
                || r == VIEW_GEOMETRY_CARGO || r == PATHS || r == HITPOINTS
                || r == PHYSX || r == BUOYANCY;
        }

        public static LodName GetLODType(this float res)
        {
            if (res == specialLod) return LodName.Memory;
            if (res == 2 * specialLod) return LodName.LandContact;
            if (res == 3 * specialLod) return LodName.Roadway;
            if (res == 4 * specialLod) return LodName.Paths;

            if (res == 5 * specialLod) return LodName.HitPoints;
            if (res == 6 * specialLod) return LodName.ViewGeometry;
            if (res == 7 * specialLod) return LodName.FireGeometry;
            if (res == 8 * specialLod) return LodName.ViewCargoGeometry;
            if (res == 9 * specialLod) return LodName.ViewCargoFireGeometry;
            if (res == 10 * specialLod) return LodName.ViewCommander;
            if (res == 11 * specialLod) return LodName.ViewCommanderGeometry;
            if (res == 12 * specialLod) return LodName.ViewCommanderFireGeometry;
            if (res == 13 * specialLod) return LodName.ViewPilotGeometry;
            if (res == 14 * specialLod) return LodName.ViewPilotFireGeometry;
            if (res == 15 * specialLod) return LodName.ViewGunnerGeometry;
            if (res == 16 * specialLod) return LodName.ViewGunnerFireGeometry;
            if (res == 17 * specialLod) return LodName.SubParts;
            if (res == 18 * specialLod) return LodName.ShadowVolumeViewCargo;
            if (res == 19 * specialLod) return LodName.ShadowVolumeViewPilot;
            if (res == 20 * specialLod) return LodName.ShadowVolumeViewGunner;
            if (res == 21 * specialLod) return LodName.Wreck;

            if (res == 1000.0f) return LodName.ViewGunner;
            if (res == 1100.0f) return LodName.ViewPilot;
            if (res == 1200.0f) return LodName.ViewCargo;

            if (res == 1e13f) return LodName.Geometry;
            if (res == 4e13f) return LodName.PhysX;

            if (res >= 10000.0 && res <= 20000.0) return LodName.ShadowVolume;

            return LodName.Resolution;
        }

        public static string GetLODName(this float res)
        {
            var lodType = res.GetLODType();

            if (lodType == LodName.Resolution)
                return res.ToString("0.000");
            if (lodType == LodName.ShadowVolume)
                return "ShadowVolume" + (res - 10000f).ToString("0.000");
            else
                return Enum.GetName(typeof(LodName), lodType);
        }

        public static bool IsResolution(float r)
        {
            return r < SHADOW_MIN;
        }

        public static bool IsShadow(float r)
        {
            return (r >= SHADOW_MIN && r < SHADOW_MAX) ||
                r == SHADOWVOLUME_GUNNER ||
                r == SHADOWVOLUME_PILOT ||
                r == SHADOWVOLUME_CARGO;
        }

        public static bool IsVisual(float r)
        {
            return IsResolution(r) ||
                r == VIEW_CARGO ||
                r == VIEW_GUNNER ||
                r == VIEW_PILOT ||
                r == VIEW_COMMANDER;
        }
    }
}

namespace BIS.PAA.Encoder
{
    public enum PAAFlags
    {
        /// <summary>
        /// 
        /// </summary>
        None = 0,

        /// <summary>
        /// Interpolated alpha channel (default behaviour)
        /// </summary>
        InterpolatedAlpha = 1,

        /// <summary>
        /// Alpha channel interpolation disabled
        /// </summary>
        KeepAlphaAsIs = 2
    }
}
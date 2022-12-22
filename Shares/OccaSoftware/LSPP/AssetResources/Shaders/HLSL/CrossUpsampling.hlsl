#ifndef CROSSUPSAMPLE_INCLUDED
#define CROSSUPSAMPLE_INCLUDED

half4 CrossSample(Texture2D Tex, SamplerState Sampler, half2 UV, half2 SourceScale, half Ratio)
{
	half2 invScale = 1.0 / SourceScale;
	invScale *= Ratio;
	half2 p[5];
	p[0] = UV;
	p[1] = half2(UV.x + invScale.x, UV.y);
	p[2] = half2(UV.x - invScale.x, UV.y);
	p[3] = half2(UV.x, UV.y + invScale.y);
	p[4] = half2(UV.x, UV.y - invScale.y);
	
	half4 r = 0;
	for (int a = 0; a < 5; a++)
	{
		r+=Tex.SampleLevel(Sampler, p[a], 0);
	}
	
	return r * 0.2;
}

SamplerState linear_clamp_sampler;
void DoCrossUpscale_half(Texture2D Tex, half2 UV, half2 SourceScale, out half4 RGBA, out half3 RGB, out half A)
{
	RGBA = 0;
	RGB = 0;
	A = 0;
	
	#ifndef SHADERGRAPH_PREVIEW
	RGBA = CrossSample(Tex, linear_clamp_sampler, UV, SourceScale, 2.0);
	RGB = RGBA.rgb;
	A = RGBA.a;
	#endif
}

#endif
using Microsoft.FluentUI.AspNetCore.Components;

namespace TripleDerby.Web.Icons;

public class MaleIcon() : Icon("MaleIcon", IconVariant.Regular, IconSize.Custom, SvgContent)
{
    private const string SvgContent = """
<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 20 20"><path d="M7,18.005 C4.243,18.005 2,15.762 2,13.005 C2,10.248 4.243,8.005 7,8.005 C9.757,8.005 12,10.248 12,13.005 C12,15.762 9.757,18.005 7,18.005 L7,18.005 Z M12,0 L12,2 L16.586,2 L11.186,7.402 C10.018,6.527 8.572,6.004 7,6.004 C3.134,6.004 0,9.138 0,13.004 C0,16.87 3.134,20.005 7,20.005 C10.866,20.005 14,16.871 14,13.005 C14,11.433 13.475,9.987 12.601,8.818 L18,3.419 L18,8 L20,8 L20,0 L12,0 Z" id="male-[#1364]"/></svg>
""";
}
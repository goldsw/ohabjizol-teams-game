using System;
using System.Collections.Generic;
namespace Ohabjizol.Interview
{
    public static class PortfolioResponseCatalog
    {
        static readonly IReadOnlyList<string> Good = Array.AsReadOnly(new[] {
            "(좋음 설명) 꽤 실력 있는 지원자인 것 같다.",
            "(좋음 설명) 프로젝트의 완성도가 높고, 맡은 작업을 능숙하게 해낸 것 같다.",
            "(좋음 설명) 작업 과정과 개선 결과를 보니 탄탄한 실력을 갖춘 것으로 보인다."
        });
        static readonly IReadOnlyList<string> Normal = Array.AsReadOnly(new[] {
            "(보통 설명) 중급 정도의 실력인 것 같다.",
            "(보통 설명) 프로젝트의 기본 구성은 갖췄지만, 완성도는 조금 더 다듬어야 할 것 같다.",
            "(보통 설명) 맡은 작업을 마무리할 실력은 있지만, 세부적인 부분은 아직 아쉬워 보인다."
        });
        static readonly IReadOnlyList<string> Bad = Array.AsReadOnly(new[] {
            "(나쁨 설명) 아직 실력이 약간 부족한 것 같다.",
            "(나쁨 설명) 프로젝트에 미완성인 부분이 많아, 작업을 끝까지 완성하는 능력이 부족해 보인다.",
            "(나쁨 설명) 결과물과 작업 과정이 충분히 정리되지 않아, 기본 실력을 더 쌓아야 할 것 같다."
        });
        public static IReadOnlyList<string> GetResponses(PortfolioDisplayState quality)
        {
            switch (quality) {
                case PortfolioDisplayState.Good: return Good;
                case PortfolioDisplayState.Normal: return Normal;
                case PortfolioDisplayState.Bad: return Bad;
                default: throw new ArgumentOutOfRangeException(nameof(quality));
            }
        }
        public static string Pick(PortfolioDisplayState quality, Random random)
        {
            if (random == null) throw new ArgumentNullException(nameof(random));
            var responses = GetResponses(quality);
            return responses[random.Next(responses.Count)];
        }
    }
}
using System;
using System.Collections.Generic;

namespace Puzzle.Core
{
    /// <summary>
    /// 리플레이 재생에 필요한 모든 데이터를 담는 직렬화 가능한 클래스입니다.
    /// 동일한 시드와 입력 시퀀스를 재현하면 결정론적으로 같은 게임 결과를 얻을 수 있습니다.
    /// </summary>
    [Serializable]
    public class ReplayData
    {
        /// <summary> 규칙 JSON의 Addressable 에셋 주소 </summary>
        public string ruleAddress;

        /// <summary> 스테이지 JSON의 Addressable 에셋 주소 </summary>
        public string stageAddress;

        /// <summary> 게임에 사용된 난수 시드 </summary>
        public int randomSeed;

        /// <summary> 유저의 입력(클릭/터치) 기록 리스트 </summary>
        public List<InputRecord> inputs;

        /// <summary> 유저의 입력 종료(포인터 릴리즈) 기록 리스트 </summary>
        public List<InputEndRecord> inputEnds;

        /// <summary> 리플레이가 기록된 일시 (ISO 8601 형식) </summary>
        public string recordedAt;
    }
}

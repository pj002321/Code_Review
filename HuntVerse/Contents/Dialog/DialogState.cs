using UnityEngine;

namespace Hunt
{
    public enum DialogState
    {
        None,               // -
        Typing,             // 대사 입력 중
        WaitingForIput,     // 입력 대기
        ShowingChoices,     // 선택 표시
        ProcessingChoice,   // 선택 처리
        Completed           // 대사 완료
    }
}

// 순간 정지장 등으로 멈추거나, 지연 방출 등으로 느려질 수 있는 대상
public interface IFreezable
{
    void Freeze(float seconds);
    // ratio = 이동 속도 감소율(0.25 = −25%). 중첩 없이 남은 시간이 긴 쪽으로 갱신
    void Slow(float ratio, float seconds);
}

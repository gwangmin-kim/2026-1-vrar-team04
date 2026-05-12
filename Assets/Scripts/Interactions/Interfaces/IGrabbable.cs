/// <summary>
/// 잡고 놓을 때 로직이 필요한 오브젝트
/// 오브젝트 자체가 XR Grabbable을 갖는 것은 아님. Grab과 Release 로직이 추가로 필요한 경우를 위한 인터페이스
/// </summary>
public interface IGrabbable
{
    public void Grab();
    public void Release();
}

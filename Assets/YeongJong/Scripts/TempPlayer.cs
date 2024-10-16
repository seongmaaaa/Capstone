using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어(라인 끝지점, 가장 최근 지점)가 라인 생성지점(초기 지점)에 접근하면 라인 사라짐 + 일정 시간이 지나면 라인 초기화
public class TempPlayer : MonoBehaviour
{
    public float speed = 280;     // 이동 속도
    public float turnSpeed = 180;  // 회전 속도
    Rigidbody2D rigid;

    [SerializeField] LineRenderer lineRenderer;     // 경로를 시각적으로 표시할 라인 렌더러
    [SerializeField] List<Vector2> points = new List<Vector2>();  // 플레이어가 지나간 경로 좌표 저장
    public int maxPoints = 200;  // 좌표의 개수를 제한
    bool trailClosed = false;     // 경로가 닫혔는지 여부 확인
    [SerializeField] float closeDistance = 0.5f;    // 경로 닫기 감지 거리
    [SerializeField] int minimumPoints = 3;          // 경로가 닫히기 위해 필요한 최소 점 개수
    [SerializeField] float minimumPathLength = 1.0f; // 경로가 닫히기 위한 최소 경로 길이 (지정하지 않으면 라인이 바로 지워짐)

    public PolygonCollider2D polygonCollider;           // 경로가 닫힐 때 사용될 콜라이더 (월드 좌표 기준으로 콜라이더를 생성하기 위해서 DestroyZone의 컴포넌트 참조)

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        lineRenderer = GetComponentInChildren<LineRenderer>();

        lineRenderer.positionCount = 0;    // 라인 렌더러 초기화
    }

    void FixedUpdate()
    {
        rigid.velocity = transform.up * speed * Time.deltaTime;  // 머리의 움직임 설정 (로컬 오른쪽 방향)
        if (Input.GetAxis("Horizontal") != 0)
            transform.Rotate(Vector3.back * turnSpeed * Time.deltaTime * Input.GetAxis("Horizontal"));

        // 경로가 닫혔다면 더 이상 경로를 그리지 않음
        if (trailClosed) 
            return;

        Vector2 currentPos = transform.position;

        // 플레이어가 이동할 때 경로 추가
        if (points.Count == 0 || Vector2.Distance(currentPos, points[points.Count - 1]) > 0.1f)
        {
            points.Add(currentPos);
            lineRenderer.positionCount = points.Count;
            lineRenderer.SetPosition(points.Count - 1, currentPos);
        }

        // 경로를 남길 최대 개수 지정 (@@@ 일정 시간이 지나면 제거하도록 변경, 한번에 제거가 아니라 오래된 좌표부터 서서히 제거되도록 변경 @@@, TimedStamp 클래스 쓸때 기존 points 변환방법)
        if (points.Count > maxPoints)
        {
            //points.RemoveAt(0);  // 가장 오래된 좌표 제거 (0번 인덱스)
            ClearTrail();
        }

        // 최소 경로 길이를 만족하고 경로가 닫혔는지 감지 (첫 번째 점과 현재 위치의 거리가 일정 이하일 때)
        if (points.Count >= minimumPoints && CalculatePathLength() > minimumPathLength && Vector2.Distance(currentPos, points[0]) < closeDistance)
        {
            CloseTrail();
        }
    }

    // 경로가 닫혔을 때 처리하는 함수
    void CloseTrail()
    {
        trailClosed = true;     // 경로가 닫혔음을 표시

        polygonCollider.SetPath(0, points.ToArray());           // 다각형 콜라이더로 경로 설정
        polygonCollider.enabled = true;                         // 콜라이더 활성화

        //Debug.Log(polygonCollider.pathCount); // 경로 개수 확인
        //Debug.Log(polygonCollider.GetPath(0).Length); // 점 개수 확인

        //Invoke("ClearTrail", 0.2f);            // 인보크로 경로와 콜라이더 초기화
        StartCoroutine(ClearTrailRoutine());     // 코루틴으로 경로와 콜라이더 초기화
    }

    IEnumerator ClearTrailRoutine()
    {
        //yield return new WaitForEndOfFrame(); // 현재 프레임이 모두 끝날때까지 대기
        yield return null;  // 1프레임 대기

        ClearTrail();
    }

    // 라인과 콜라이더를 초기화하는 함수
    void ClearTrail()
    {
        lineRenderer.positionCount = 0;         // 라인 렌더러 초기화 (라인 지우기)

        polygonCollider.enabled = false;        // 콜라이더 경로 초기화 (콜라이더 비활성화)
        
        points.Clear();                         // 경로 좌표 리스트 초기화

        trailClosed = false;                    // 경로가 닫힌 상태 초기화
    }

    // 경로의 총 길이를 계산하는 함수
    float CalculatePathLength()
    {
        float totalLength = 0.0f;

        for (int i = 1; i < points.Count; i++)
        {
            totalLength += Vector2.Distance(points[i - 1], points[i]);
        }

        return totalLength;
    }
}

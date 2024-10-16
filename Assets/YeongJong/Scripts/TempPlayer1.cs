using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어가 라인의 아무 위치나 접근하면 콜라이더 생성 + 일정 시간이 지나면 라인 사라짐
public class TempPlayer1 : MonoBehaviour
{
    [System.Serializable]
    public class TimedPoint // 좌표와 시간을 저장하는 클래스
    {
        public Vector2 position;
        public float timestamp;

        public TimedPoint(Vector2 position, float timestamp)
        {
            this.position = position;
            this.timestamp = timestamp;
        }
    }

    public List<TimedPoint> timedPoints = new List<TimedPoint>();   // TimedPoint 객체들을 담을 리스트
    public float pointLifetime = 5.0f; // 좌표가 유지될 시간 (5초)

    public float speed = 280;     // 이동 속도
    public float turnSpeed = 180;  // 회전 속도
    Rigidbody2D rigid;

    [SerializeField] LineRenderer lineRenderer;     // 경로를 시각적으로 표시할 라인 렌더러
    [SerializeField] List<Vector2> points = new List<Vector2>();  // 플레이어가 지나간 경로 좌표 저장
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

        // // 플레이어가 이동할 때 경로 추가
        // if (points.Count == 0 || Vector2.Distance(currentPos, points[points.Count - 1]) > 0.1f)
        // {
        //     points.Add(currentPos);
        //     lineRenderer.positionCount = points.Count;
        //     lineRenderer.SetPosition(points.Count - 1, currentPos);
        // }

        // 좌표 추가 (현재 시간과 함께)
        if (timedPoints.Count == 0 || Vector2.Distance(currentPos, timedPoints[timedPoints.Count - 1].position) > 0.1f)
        {
            timedPoints.Add(new TimedPoint(currentPos, Time.time));
            lineRenderer.positionCount = timedPoints.Count;
            lineRenderer.SetPosition(timedPoints.Count - 1, currentPos);
        }

        // 일정 시간이 지난 좌표 제거
        for (int i = timedPoints.Count - 1; i >= 0; i--)
        {
            if (Time.time - timedPoints[i].timestamp > pointLifetime)
            {
                timedPoints.RemoveAt(i);  // 시간이 지난 좌표 제거
            }
        }

        // 라인 렌더러 업데이트
        lineRenderer.positionCount = timedPoints.Count;
        for (int i = 0; i < timedPoints.Count; i++)
        {
            lineRenderer.SetPosition(i, timedPoints[i].position);
        }

        // // 최소 경로 길이를 만족하고 경로가 닫혔는지 감지 (첫 번째 점과 현재 위치의 거리가 일정 이하일 때)
        // if (points.Count >= minimumPoints && CalculatePathLength() > minimumPathLength && Vector2.Distance(currentPos, points[0]) < closeDistance)
        // {
        //     CloseTrail();
        // }

        // // 경로가 닫혔는지 감지 (경로 내의 어느 점에 도달했는지 확인)
        // if (points.Count >= minimumPoints && CalculatePathLength() > minimumPathLength)
        // {
        //     int closestIndex = FindClosestPoint(currentPos); // 가장 가까운 경로 지점의 인덱스 찾기
        //     if (Vector2.Distance(currentPos, points[closestIndex]) < closeDistance)
        //     {
        //         Debug.Log("가장 가까운 지점 인덱스" + closestIndex);
        //         CloseTrail(closestIndex);  // 가장 가까운 지점으로 경로를 닫음
        //     }
        // }

        // 경로가 닫혔는지 감지 (경로 내의 어느 점에 도달했는지 확인)
        if (timedPoints.Count >= minimumPoints && CalculatePathLength() > minimumPathLength)
        {
            int closestIndex = FindClosestPoint(currentPos); // 가장 가까운 경로 지점의 인덱스 찾기
            if (Vector2.Distance(currentPos, timedPoints[closestIndex].position) < closeDistance)
            {
                Debug.Log("가장 가까운 지점 인덱스" + closestIndex);
                CloseTrail(closestIndex);  // 가장 가까운 지점으로 경로를 닫음
            }
        }

    }

    // 경로에서 플레이어와 가장 가까운 점을 찾는 함수
    int FindClosestPoint(Vector2 currentPos)
    {
        int closestIndex = 0;
        float closestDistance = Vector2.Distance(currentPos, timedPoints[0].position);

        for (int i = 1; i < timedPoints.Count - 5; i++)
        {
            float distance = Vector2.Distance(currentPos, timedPoints[i].position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        //Debug.Log("closetIndex : " + closestIndex);
        return closestIndex;
    }

    // 경로가 닫혔을 때 처리하는 함수
    void CloseTrail(int closeIndex)
    {
        trailClosed = true;     // 경로가 닫혔음을 표시

        //polygonCollider.SetPath(0, points.ToArray());           // 다각형 콜라이더로 경로 설정

        
        List<Vector2> closedPath = new List<Vector2>(timedPoints.GetRange(closeIndex, timedPoints.Count - closeIndex).ConvertAll(tp => tp.position)); // 닫힌 지점부터 현재 위치까지의 경로만 사용
        // @@ 람다 표현식, LINQ 메서드

        polygonCollider.SetPath(0, closedPath.ToArray());       // 닫힌 경로를 PolygonCollider2D로 설정

        polygonCollider.enabled = true;                         // 콜라이더 활성화

        //Debug.Log(polygonCollider.pathCount); // 경로 개수 확인
        //Debug.Log(polygonCollider.GetPath(0).Length); // 점 개수 확인

        //Invoke("ClearTrail", 1f);            // 인보크로 경로와 콜라이더 초기화
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
        
        timedPoints.Clear();                         // 경로 좌표 리스트 초기화

        trailClosed = false;                    // 경로가 닫힌 상태 초기화
    }

    // 경로의 총 길이를 계산하는 함수
    float CalculatePathLength()
    {
        float totalLength = 0.0f;

        for (int i = 1; i < timedPoints.Count; i++)
        {
            totalLength += Vector2.Distance(timedPoints[i - 1].position, timedPoints[i].position);
        }

        return totalLength;
    }
}

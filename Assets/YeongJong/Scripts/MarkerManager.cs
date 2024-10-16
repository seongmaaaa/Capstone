using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MarkerManager : MonoBehaviour
{
    [System.Serializable]
    public class Marker    // 마커 클래스 
    {
        public Vector3 position;        // 마커 생성 시 위치값
        public Quaternion rotation;     // 마커 생성 시 회전값

        public Marker(Vector3 pos, Quaternion rot)  // 생성자
        {
            position = pos;
            rotation = rot;
        }
    }
    
    public List<Marker> markerList = new List<Marker>();    // 마커 객체들을 담을 리스트
    
    void Start()
    {
        
    }

    void Update()
    {
        
    }

    void FixedUpdate() 
    {
        UpdateMarkerList();
    }

    public void UpdateMarkerList()
    {
        markerList.Add(new Marker(transform.position, transform.rotation));
    }

    public void ClearMarkerList()   // 마커 리스트 초기화
    {
        markerList.Clear();
        markerList.Add(new Marker(transform.position, transform.rotation));
    }


}

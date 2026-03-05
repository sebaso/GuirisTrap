using UnityEngine;
using UnityEngine.EventSystems;

public class GridController : MonoBehaviour
{
    [SerializeField]
    private Camera _mainCamera;
    [SerializeField] 
    private LayerMask _floorMask;
    private bool _hasObjectSelected = false;
    PlaceableObject placeableObject;
    [SerializeField]
    private GridData _gridData;

    public void Update()
    {
        SelectPlaceableObject();
        if(placeableObject != null)
        {
            MovePlaceableObject();
            PlacePlaceableObject();
        }
    }

    private void SelectPlaceableObject()
    {
        if (Input.GetMouseButtonDown(0) && !_hasObjectSelected)
        {
            Vector3 moussePos = Input.mousePosition;
            Ray ray = _mainCamera.ScreenPointToRay(moussePos);

            RaycastHit hitInfo;

            bool hit = Physics.Raycast(ray, out hitInfo);
            if (hit)
            {
                placeableObject = hitInfo.transform.GetComponent<PlaceableObject>();
                if (placeableObject && !placeableObject.IsSelected())
                {
                    _hasObjectSelected = true;
                    placeableObject.Select(true);
                }
            }
        }
    }
    private void MovePlaceableObject()
    {
       if (placeableObject && _hasObjectSelected)
        {
            if(placeableObject.IsSelected())
            {
                Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
                if(Physics.Raycast(ray, out RaycastHit hit, 1000f, _floorMask))
                {
                    placeableObject.transform.position = hit.point;
                    placeableObject.GetComponent<Collider>().enabled = false;
                }
            }
        }
    }
    private void PlacePlaceableObject()
    {
        if(placeableObject && _hasObjectSelected)
        {
            if (placeableObject.IsSelected() && Input.GetMouseButtonUp(0))
            {
                if(_gridData.GetType(placeableObject.CurrentCellX, placeableObject.CurrentCellY) == CellType.Empty)
                {
                    Vector3 pos = new Vector3(placeableObject.CurrentCellX + 0.5f, 0f, placeableObject.CurrentCellY + 0.5f);
                    placeableObject.transform.position = pos;
                    placeableObject.GetComponent<Collider>().enabled = true;
                    _hasObjectSelected = false;
                    placeableObject.Select(false);
                }
            }
        }
    }
}

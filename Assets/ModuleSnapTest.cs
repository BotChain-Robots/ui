using UnityEngine;

public class ModuleSnapTest : MonoBehaviour
{
    public Transform socketA;  // e.g. BodySocket of Module A
    public Transform socketB;  // e.g. ArmSocket of Module B

    void Start()
    {
        SnapModule();
    }

    void SnapModule()
{
    Transform moduleA = socketA.root;  // root of module A
    Transform moduleB = socketB.root;  // root of module B

    // Step 1: Calculate rotation needed to align socketA to socketB (facing opposite directions)
    Quaternion targetRotation = Quaternion.LookRotation(-socketB.forward, socketB.up);
    Quaternion rotationOffset = Quaternion.Inverse(socketA.rotation) * targetRotation;

    // Step 2: Rotate module A
    moduleA.rotation = rotationOffset * moduleA.rotation;

    // Step 3: Recalculate socketA position after rotation
    Vector3 socketAWorldPos = socketA.position;
    Vector3 positionOffset = socketAWorldPos - moduleA.position;

    // Step 4: Move module A so socketA aligns with socketB
    moduleA.position = socketB.position - positionOffset;
}
}

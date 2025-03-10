using UnityEngine;

public class EncapsulationStuff
{
    // can be accessed from anywhere
    public int thingForAll;
    // can't be accessed from outside class
    private int thing;
    // counts as private
    int thingUnspecified;
    // can be accessed from editor but not other classes
    [SerializeField]
    private int editorThing;
    // can be accessed from other classes like public, usually basically the same
    public int encapsulatedThing { get; set; }

    // what the above actually does; turns it into a property (capitalized) which can be modified to add restrictions etc if we don't want the value to be modified freely
    private int encapsulatedThingBig;
    public int EncapsulatedThingBig
    {
        get
        {
            return this.encapsulatedThingBig;
        }
        set
        {
            this.encapsulatedThingBig = value;
        }
    }

    // example of adding restrictions
    private int encapsulatedThingBut;
    public int EncapsulatedThingBut
    {
        get
        {
            return this.encapsulatedThingBut;
        }
        set
        {
            // only set the value if trying to set it to less than 100
            if (value < 100)
                this.encapsulatedThingBut = value;
        }
    }
}

public class MoreEncapsulation
{ 
    // reference to the class above
    EncapsulationStuff encapsulationStuff;
    public void SetEncapsulatedThing()
    {
        // property can be set/got like a regular int
        encapsulationStuff.EncapsulatedThingBig = 1000;
        int value = encapsulationStuff.EncapsulatedThingBig;

        // this wouldn't assign, because the value exceeds 99
        encapsulationStuff.EncapsulatedThingBut = value;
    }
}

public class TDSound {
    private int id;
    private string name;
    private string sound;


    public TDSound() {
        id = 0;
        name = "Silence";
        sound = "tdm_silence";
    }

    public TDSound(int id, string name, string sound) {
        this.id = id;
        this.name = name;
        this.sound = sound;
    }


    public int GetId() {
        return id;
    }

    public string GetName() {
        return name;
    }

    public string GetSound() {
        return sound;
    }
}

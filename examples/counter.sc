class Counter {
    int value = 0;

    void Inc(int by) {
        this.value = this.value + by;
    }

    int Get() {
        return this.value;
    }
}

var counter = new Counter();
counter.Inc(5);
counter.Inc(2);
Console.WriteLine("count =", counter.Get());

Thread t = new(delegate ()
{
    Server myserver = new("0.0.0.0", 10001);
});
t.Start();

Console.WriteLine("Server Started...!");
